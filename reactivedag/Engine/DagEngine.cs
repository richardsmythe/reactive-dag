using System;
using ReactiveDAG.Core.Models;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;

namespace ReactiveDAG.Core.Engine
{
    /// <summary>
    /// The main engine that manages the execution of the Directed Acyclic Graph (DAG).
    /// Handles the creation, updates, and removal of nodes and manages the dependencies between cells.
    /// </summary>
    public partial class DagEngine : IDisposable
    {
        private readonly ConcurrentDictionary<int, IDagNodeOperations> _nodes = new ConcurrentDictionary<int, IDagNodeOperations>();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private int _nextIndex = 0;
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, byte>> _dependentsMap = new();
        private bool _disposed;

        private void AddDependentsMapEdge(int dependencyIndex, int dependentIndex)
        {
            var set = _dependentsMap.GetOrAdd(dependencyIndex, _ => new ConcurrentDictionary<int, byte>());
            set[dependentIndex] = 0;
        }

        private void RemoveDependentsMapEdge(int dependencyIndex, int dependentIndex)
        {
            if (_dependentsMap.TryGetValue(dependencyIndex, out var set))
            {
                set.TryRemove(dependentIndex, out _);
                if (set.Count == 0)
                {
                    _dependentsMap.TryRemove(dependencyIndex, out _);
                }
            }
        }

        /// <summary>
        /// Returns the node
        /// </summary>
        public DagNode<T> GetNode<T>(Cell<T> cell)
        {
            ThrowIfDisposed();
            if (!_nodes.TryGetValue(cell.Index, out var node))
            {
                throw new InvalidOperationException("Node not found.");
            }
            return (DagNode<T>)node;
        }

        /// <summary>
        /// Gets the indices of nodes that depend on the specified node index.
        /// </summary>
        private IEnumerable<int> GetDependentNodes(int index)
        {
            if (_dependentsMap.TryGetValue(index, out var set))
            {
                return set.Keys.ToArray();
            }
            return Array.Empty<int>();
        }

        /// <summary>
        /// Returns all nodes in the DAG.
        /// </summary>
        public IEnumerable<IDagNodeOperations> GetAllNodes()
        {
            ThrowIfDisposed();
            return _nodes.Values;
        }

        /// <summary>
        /// Gets the total number of nodes in the DAG.
        /// </summary>
        public int NodeCount
        {
            get
            {
                ThrowIfDisposed();
                return _nodes.Count;
            }
        }

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
        public async Task<T> GetResult<T>(Cell<T> cell)
        {
            ThrowIfDisposed();
            if (!_nodes.TryGetValue(cell.Index, out var node)) throw new InvalidOperationException("Node not found.");
            var dagNode = (DagNode<T>)node;

            // Check for reentrancy
            if (node.IsComputing())
            {
                // Build dependency chain for debugging
                string chain = BuildDependencyChain(cell.Index);
                throw new InvalidOperationException($"Reentrancy detected: node {cell.Index} is currently being computed. Dependency chain: {chain}");
            }

            var result = await dagNode.DeferredComputedNodeValue.Value;
            return result;
        }

        private string BuildDependencyChain(int startIndex)
        {
            var chain = new List<int> { startIndex };
            var current = startIndex;
            var visited = new HashSet<int> { startIndex };

            while (true)
            {
                if (!_nodes.TryGetValue(current, out var node)) break;

                var deps = node.GetDependencies();
                if (deps == null || deps.Count == 0) break;

                int? next = null;
                foreach (var d in deps)
                {
                    if (!visited.Contains(d)) { next = d; break; }
                }
                if (next == null) break;

                chain.Add(next.Value);
                visited.Add(next.Value);
                current = next.Value;
            }

            return string.Join(" -> ", chain);
        }

        /// <summary>
        /// Streams the result of a specific cell asynchronously.
        /// The result is yielded whenever it changes.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="cell">The cell whose result is to be streamed.</param>
        /// <param name="cancellationToken">Cancellation token to stop the streaming.</param>
        /// <returns>An asynchronous stream of results for the cell.</returns>
        public async IAsyncEnumerable<T> StreamResults<T>(Cell<T> cell, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (!_nodes.TryGetValue(cell.Index, out var node))
                throw new InvalidOperationException("Node not found.");

            var dagNode = (DagNode<T>)node;
            var channel = Channel.CreateBounded<T>(new BoundedChannelOptions(1)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            async void Handler()
            {
                try
                {
                    if (cts.IsCancellationRequested) return;
                    var newValue = await GetResult(cell).ConfigureAwait(false);
                    await channel.Writer.WriteAsync(newValue, cts.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    channel.Writer.TryComplete(ex);
                    cts.Cancel();
                }
            }
            dagNode.NodeUpdated += Handler;
            try
            {
                var initialValue = await GetResult(cell).ConfigureAwait(false);
                await channel.Writer.WriteAsync(initialValue, cts.Token).ConfigureAwait(false);
                await foreach (var value in channel.Reader.ReadAllAsync(cts.Token).ConfigureAwait(false))
                {
                    yield return value;
                }
            }
            finally
            {
                cts.Cancel();
                dagNode.NodeUpdated -= Handler;
                channel.Writer.TryComplete();
                cts.Dispose();
            }

        }

        /// <summary>
        /// Removes a node from the DAG and cleans up its dependencies.
        /// </summary>
        /// <param name="cell">The cell whose node is to be removed.</param>
        public void RemoveNode<T>(Cell<T> cell)
        {
            ThrowIfDisposed();
            if (_nodes.TryRemove(cell.Index, out var node))
            {
                ((DagNode<T>)node).DisposeSubscriptions();

                var dependentIndices = GetDependentNodes(cell.Index).ToList();


                foreach (var dependentIndex in dependentIndices)
                {
                    if (_nodes.TryGetValue(dependentIndex, out var dependentNode))
                    {
                        dependentNode.RemoveDependency(cell.Index);
                        RemoveDependentsMapEdge(cell.Index, dependentIndex);
                    }
                }


                foreach (var dep in node.GetDependencies())
                {
                    RemoveDependentsMapEdge(dep, cell.Index);
                }


                foreach (var dependentIndex in dependentIndices)
                {
                    if (_nodes.TryGetValue(dependentIndex, out var dependentNode))
                    {
                        dependentNode.ResetComputation();
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new input cell to the DAG.
        /// </summary>
        /// <typeparam name="T">The type of the value for the cell.</typeparam>
        /// <param name="value">The value of the input cell.</param>
        /// <returns>The created input cell.</returns>
        public Cell<T> AddInput<T>(T value)
        {
            ThrowIfDisposed();
            var cell = Cell<T>.CreateInputCell(_nextIndex++, value);
            var node = new DagNode<T>(cell, () => Task.FromResult(value));
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
        public Cell<TResult> AddFunction<TInputs, TResult>(Cell<TInputs>[] inputCells, Func<TInputs[], Task<TResult>> asyncFunction)
        {
            ThrowIfDisposed();
            var cell = Cell<TResult>.CreateFunctionCell(_nextIndex++);


            foreach (var c in inputCells)
            {
                if (c.Index == cell.Index)
                    throw new InvalidOperationException("A node cannot depend on itself (Lazy reentrancy protection).");
            }

            var node = new DagNode<TResult>(cell, async () =>
            {

                foreach (var dep in inputCells)
                {
                    if (_nodes.TryGetValue(dep.Index, out var depNode) && depNode.IsComputing())
                    {
                        throw new InvalidOperationException($"Reentrancy detected in dependency node {dep.Index} while computing node {cell.Index}.");
                    }
                }

                var inputValues = await Task.WhenAll(inputCells.Select(c => GetResult(c)));
                var result = await asyncFunction(inputValues);
                return result;
            });

            _nodes[cell.Index] = node;

            foreach (var c in inputCells)
            {
                if (!_nodes.ContainsKey(c.Index))
                    throw new InvalidOperationException($"Dependency cell with index {c.Index} not found.");

                node.Dependencies.Add(c.Index);
                AddDependentsMapEdge(c.Index, cell.Index);
            }

            var dependencyCells = inputCells.Cast<BaseCell>();
            node.ConnectDependencies(dependencyCells, node.ComputeNodeValueAsync);

            // ensure no cycles exist after dependencies are set
            foreach (var c in inputCells)
            {
                if (IsCyclic(c.Index, cell.Index))
                    throw new InvalidOperationException($"Cycle detected after node creation: {c.Index} -> {cell.Index}");
            }

            return cell;
        }

        /// <summary>
        /// Adds a function node to the DAG that computes a result based on mixed-type input cells (BaseCell[]).
        /// </summary>
        /// <typeparam name="TResult">The result type of the function.</typeparam>
        /// <param name="dependencies">The cells this function depends on.</param>
        /// <param name="function">The function to compute the result based on input cells.</param>
        /// <returns>The created function cell.</returns>
        public Cell<TResult> AddFunction<TResult>(BaseCell[] dependencies, Func<object[], Task<TResult>> function)
        {
            ThrowIfDisposed();
            var cell = Cell<TResult>.CreateFunctionCell(_nextIndex++);
            var node = new DagNode<TResult>(cell, async () =>
            {
                var inputValues = dependencies.Select(dep =>
                {
                    var prop = dep.GetType().GetProperty("Value");
                    return prop?.GetValue(dep);
                }).ToArray();
                return await function(inputValues);
            });

            node.Dependencies = dependencies.Select(d => d.Index).ToHashSet();
            _nodes[cell.Index] = node;
            foreach (var d in dependencies)
            {
                AddDependentsMapEdge(d.Index, cell.Index);
            }
            node.ConnectDependencies(dependencies, node.ComputeNodeValueAsync);


            foreach (var d in dependencies)
            {
                if (IsCyclic(d.Index, cell.Index))
                    throw new InvalidOperationException($"Cycle detected after node creation: {d.Index} -> {cell.Index}");
            }
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
            ThrowIfDisposed();
            var visited = new HashSet<int>();
            bool dfs(int current)
            {
                if (current == targetIndex) return true;
                if (!visited.Add(current)) return false;
                if (_nodes.TryGetValue(current, out var node))
                {
                    foreach (var dependency in node.GetDependencies())
                    {
                        if (dfs(dependency)) return true;
                    }
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
            ThrowIfDisposed();
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
            ThrowIfDisposed();
            if (!EqualityComparer<T>.Default.Equals(cell.Value, value))
            {
                cell.PreviousValue = cell.Value;
                cell.Value = value;

                if (_nodes.TryGetValue(cell.Index, out var node) && node is DagNode<T> typedNode)
                {

                    typedNode.DeferredComputedNodeValue = new Lazy<Task<T>>(() => Task.FromResult(value), LazyThreadSafetyMode.ExecutionAndPublication);


                    node.NotifyUpdatedNode();

                    await UpdateAndRefresh(cell.Index);
                }
            }
        }

        /// <summary>
        /// Updates and refreshes the nodes based on the provided mode.
        /// </summary>
        /// <param name="startIndex">The starting node index for the update.</param>
        private async Task UpdateAndRefresh(int startIndex)
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

                    if (!_nodes.TryGetValue(index, out var node)) continue;

                    var dependentIndices = GetDependentNodes(index).ToList();

                    await node.EvaluateAsync();

                    foreach (var dependentIndex in dependentIndices)
                    {
                        if (_nodes.TryGetValue(dependentIndex, out var dependentNode))
                        {
                            dependentNode.ResetComputation();

                            await dependentNode.EvaluateAsync();

                            dependentNode.NotifyUpdatedNode();

                            stack.Push(dependentIndex);
                        }
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private object GetCellValue(BaseCell cell)
        {
            // Use reflection to get the Value property from the cell
            var type = cell.GetType();
            var valueProp = type.GetProperty("Value");
            return valueProp?.GetValue(cell);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DagEngine));
            }
        }

        /// <summary>
        /// Releases resources held by the DagEngine.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _lock.Wait();
                try
                {
                    foreach (var node in _nodes.Values.ToList())
                    {
                        node.DisposeSubscriptions();
                    }
                    _nodes.Clear();
                    _dependentsMap.Clear();
                }
                finally
                {
                    _lock.Release();
                    _lock.Dispose();
                }
            }

            _disposed = true;
        }
    }
}