using ReactiveDAG.Core.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ReactiveDAG.Core.Engine
{
    /// <summary>
    /// Enum representing the update modes for the DAG engine.
    /// </summary>
    public enum UpdateMode
    {
        Update,
        RefreshDependencies
    }

    /// <summary>
    /// The main engine that manages the execution of the Directed Acyclic Graph (DAG).
    /// Handles the creation, updates, and removal of nodes and manages the dependencies between cells.
    /// </summary>
    public class DagEngine
    {
        private readonly ConcurrentDictionary<int, DagNode> _nodes = new ConcurrentDictionary<int, DagNode>();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        private int _nextIndex = 0;
        /// <summary>
        /// Gets the total number of nodes in the DAG.
        /// </summary>
        public int NodeCount => _nodes.Count;

        /// <summary>
        /// Delegate for handling result callbacks.
        /// </summary>
        public delegate void ResultCallback<TResult>(TResult result);

        /// <summary>
        /// Retrieves the result for a specific cell asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="cell">The cell whose result is to be retrieved.</param>
        /// <returns>The result of the cell.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the node for the given cell is not found.</exception>
        public async Task<T> GetResult<T>(BaseCell cell)
        {
            if (!_nodes.TryGetValue(cell.Index, out var node))
            {
                throw new InvalidOperationException("Node not found.");
            }
            var result = await node.DeferredComputedNodeValue.Value;
            return (T)result;
        }

        /// <summary>
        /// Streams the result of a specific cell asynchronously.
        /// The result is yielded whenever it changes.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="cell">The cell whose result is to be streamed.</param>
        /// <param name="cancellationToken">Cancellation token to stop the streaming.</param>
        /// <returns>An asynchronous stream of results for the cell.</returns>
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

        /// <summary>
        /// Removes a node from the DAG and cleans up its dependencies.
        /// </summary>
        /// <param name="cell">The cell whose node is to be removed.</param>
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


        /// <summary>
        /// Adds a new input cell to the DAG.
        /// </summary>
        /// <typeparam name="T">The type of the value for the cell.</typeparam>
        /// <param name="value">The value of the input cell.</param>
        /// <returns>The created input cell.</returns>
        public Cell<T> AddInput<T>(T value)
        {
            var cell = Cell<T>.CreateInputCell(_nextIndex++, value);
            var node = new DagNode(cell, () => Task.FromResult<object>(value));
            _nodes[cell.Index] = node;
            return cell;
        }

        /// <summary>
        /// Adds a function node to the DAG, which is a computation dependent on other cells.
        /// </summary>
        /// <typeparam name="TResult">The result type of the function.</typeparam>
        /// <param name="cells">The cells this function depends on.</param>
        /// <param name="function">The function to compute the result based on input cells.</param>
        /// <returns>The created function cell.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a dependency is not found or a cyclic dependency is detected.</exception>
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


        /// <summary>
        /// Checks if there is a cyclic dependency between two nodes in the DAG.
        /// </summary>
        /// <param name="startIndex">The starting node index.</param>
        /// <param name="targetIndex">The target node index.</param>
        /// <returns>True if a cyclic dependency is found, otherwise false.</returns>
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

        /// <summary>
        /// Checks if the value of a given cell has changed.
        /// </summary>
        /// <typeparam name="T">The type of the cell's value.</typeparam>
        /// <param name="cell">The cell to check for changes.</param>
        /// <returns>True if the value has changed, otherwise false.</returns>
        public bool HasChanged<T>(Cell<T> cell)
        {
            return !EqualityComparer<T>.Default.Equals(cell.PreviousValue, cell.Value);
        }

        /// <summary>
        /// Updates the value of an input cell in the DAG and triggers updates for dependent nodes.
        /// </summary>
        /// <typeparam name="T">The type of the cell's value.</typeparam>
        /// <param name="cell">The cell to update.</param>
        /// <param name="value">The new value to set for the cell.</param>
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

        /// <summary>
        /// Updates and refreshes the nodes based on the provided mode.
        /// </summary>
        /// <param name="startIndex">The starting node index for the update.</param>
        /// <param name="mode">The update mode (update or refresh dependencies).</param>
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


