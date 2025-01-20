using ReactiveDAG.Core.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ReactiveDAG.Core.Engine
{
    public enum UpdateMode
    {
        Update,
        RefreshDependencies
    }

    public class DagEngine
    {
        private readonly ConcurrentDictionary<int, DagNode> _nodes = new ConcurrentDictionary<int, DagNode>();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        private int _nextIndex = 0;
        public int NodeCount => _nodes.Count;

        public delegate void ResultCallback<TResult>(TResult result);

        public async Task<T> GetResult<T>(BaseCell cell)
        {
            if (!_nodes.TryGetValue(cell.Index, out var node))
            {
                throw new InvalidOperationException("Node not found.");
            }
            var result = await node.DeferredComputedNodeValue.Value;
            return (T)result;
        }

        public async IAsyncEnumerable<T> GetResultStream<T>(Cell<T> cell, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            T lastResult = default;
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await GetResult<T>(cell);
                if (!EqualityComparer<T>.Default.Equals(lastResult, result))
                {
                    yield return result;
                    lastResult = result;
                }
            }
        }


        public void RemoveNode(BaseCell cell)
        {
            if (_nodes.TryRemove(cell.Index, out var node))
            {
                node.DisposeSubscriptions();
                foreach (var dependentIndex in GetDependentNodes(cell.Index))
                {
                    _nodes[dependentIndex].Dependencies.Remove(cell.Index);
                }
                foreach (var dependentIndex in GetDependentNodes(cell.Index))
                {
                    var dependentNode = _nodes[dependentIndex];
                    dependentNode.DeferredComputedNodeValue = new Lazy<Task<object>>(dependentNode.ComputeNodeValueAsync);
                }
            }
        }

        private IEnumerable<int> GetDependentNodes(int index)
            => _nodes.Where(n => n.Value.Dependencies.Contains(index)).Select(n => n.Key);

        public Cell<T> AddInput<T>(T value)
        {
            var cell = Cell<T>.CreateInputCell(_nextIndex++, value);
            var node = new DagNode(cell, () => Task.FromResult<object>(value));
            _nodes[cell.Index] = node;
            return cell;
        }

        public Cell<TResult> AddFunction<TResult>(BaseCell[] cells, Func<object[], TResult> function)
        {
            var cell = Cell<TResult>.CreateFunctionCell(_nextIndex++);
            var node = new DagNode(cell, async () =>
            {
                var inputValues = await Task.WhenAll(cells.Select(c => _nodes[c.Index].DeferredComputedNodeValue.Value));
                var result = function(inputValues);
                return result;
            });
            _nodes[cell.Index] = node;

            foreach (var c in cells)
            {
                if (!_nodes.ContainsKey(c.Index)) throw new InvalidOperationException($"Dependency cell with index {c.Index} not found.");
                if (IsCyclic(cell.Index, c.Index)) throw new InvalidOperationException("Cyclic dependency detected.");
                node.Dependencies.Add(c.Index);
            }

            var dependencyCells = cells.OfType<Cell<object>>();
            node.ConnectDependencies(dependencyCells, node.ComputeNodeValueAsync);
            return cell;
        }

        public bool IsCyclic(int startIndex, int targetIndex)
        {
            var visited = new HashSet<int>();
            bool dfs(int current)
            {
                if (current == targetIndex) return true;
                if (!visited.Add(current)) return false;
                foreach (var dependency in _nodes[current].Dependencies)
                {
                    if (dfs(dependency)) return true;
                }
                return false;
            }
            return dfs(startIndex);
        }

        public bool HasChanged<T>(Cell<T> cell)
        {
            return !EqualityComparer<T>.Default.Equals(cell.PreviousValue, cell.Value);
        }

        public async Task UpdateInput<T>(Cell<T> cell, T value)
        {
            if (!EqualityComparer<T>.Default.Equals(cell.Value, value))
            {
                cell.PreviousValue = cell.Value;
                cell.Value = value;
                var node = _nodes[cell.Index];
                node.DeferredComputedNodeValue = new Lazy<Task<object>>(() => Task.FromResult<object>(value));
                await UpdateAndRefresh(cell.Index, UpdateMode.Update);
            }
        }

        private async Task UpdateAndRefresh(int startIndex, UpdateMode mode)
        {
            var visited = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(startIndex);
            await _lock.WaitAsync();
            try
            {
                while (stack.Count > 0)
                {
                    var index = stack.Pop();
                    if (!visited.Add(index)) continue;

                    var node = _nodes[index];
                    var dependentNodes = GetDependentNodes(index).ToList();

                    if (mode == UpdateMode.RefreshDependencies)
                    {
                        node.DisposeSubscriptions();
                        var dependencyCells = dependentNodes
                            .Select(depIndex => _nodes[depIndex].Cell)
                            .OfType<Cell<object>>();
                        node.ConnectDependencies(dependencyCells, node.ComputeNodeValueAsync);
                    }
                    else if (mode == UpdateMode.Update)
                    {
                        foreach (var dependentNodeIndex in dependentNodes)
                        {
                            node = _nodes[dependentNodeIndex];
                            node.DeferredComputedNodeValue = new Lazy<Task<object>>(node.ComputeNodeValueAsync);
                            stack.Push(dependentNodeIndex);
                        }
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}


