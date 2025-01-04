using ReactiveDAG.Core.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;

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

        private int _nextIndex = 0;
        public int NodeCount => _nodes.Count;

        public delegate void ResultCallback<TResult>(TResult result);

        public async Task<T> GetResult<T>(BaseCell cell)
        {
            DagNode node;
            if (_nodes.TryGetValue(cell.Index, out var existingNode))
            {
                node = existingNode;
            }
            else
            {
                throw new InvalidOperationException("Node not found.");
            }
            var result = await node.DeferredComputedNodeValue.Value;
            return (T)result;
        }

        public void RemoveNode(BaseCell cell)
        {
            if (_nodes.ContainsKey(cell.Index))
            {
                var dependentCells = GetDependentNodes(cell.Index).ToList();
                foreach (var d in dependentCells)
                {
                    _nodes[d].Dependencies.Remove(cell.Index);
                }
                _nodes.TryRemove(cell.Index,out _);
            }
        }

        private IEnumerable<int> GetDependentNodes(int index)
            =>
                _nodes.Where(n => n.Value.Dependencies
                        .Contains(index))
                    .Select(n => n.Key);

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
                var inputValues =
                    await Task.WhenAll(cells.Select(c => _nodes[c.Index].DeferredComputedNodeValue.Value));
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

        public void UpdateInput<T>(Cell<T> cell, T value)
        {
            if (!EqualityComparer<T>.Default.Equals(cell.Value, value))
            {
                cell.PreviousValue = cell.Value;
                cell.Value = value;
                var node = _nodes[cell.Index];
                node.DeferredComputedNodeValue = new Lazy<Task<object>>(() => Task.FromResult<object>(value));
                UpdateAndRefresh(cell.Index, UpdateMode.Update);
            }
        }

        private void UpdateAndRefresh(int startIndex, UpdateMode mode)
        {
            var visited = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(startIndex);
            while (stack.Count > 0)
            {
                var index = stack.Pop();
                if (!visited.Add(index)) continue;
                var node = _nodes[index];
                var dependantNodes = GetDependentNodes(index);
                if (mode == UpdateMode.Update)
                {
                    foreach (var dependentNodeIndex in dependantNodes)
                    {
                        node = _nodes[dependentNodeIndex];
                        node.DeferredComputedNodeValue = new Lazy<Task<object>>(node.ComputeNodeValueAsync);
                        stack.Push(dependentNodeIndex);
                    }
                }
                else
                {
                    Console.WriteLine($"Refreshing dependencies starting from index {index}");
                    var dependentNodes = GetDependentNodes(index);
                    foreach (var dependentNodeIndex in dependentNodes)
                    {
                        stack.Push(dependentNodeIndex);
                    }
                }
            }
        }
    }
}