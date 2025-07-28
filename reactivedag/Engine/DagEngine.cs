using ReactiveDAG.Core.Models;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Newtonsoft.Json;

namespace ReactiveDAG.Core.Engine
{
    /// <summary>
    /// The main engine that manages the execution of the Directed Acyclic Graph (DAG).
    /// Handles the creation, updates, and removal of nodes and manages the dependencies between cells.
    /// </summary>
    public partial class DagEngine
    {
        private readonly ConcurrentDictionary<int, object> _nodes = new ConcurrentDictionary<int, object>();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private int _nextIndex = 0;

        /// <summary>
        /// Returns the node
        /// </summary>
        public DagNode<T> GetNode<T>(Cell<T> cell)
        {
            if (!_nodes.TryGetValue(cell.Index, out var node))
            {
                throw new InvalidOperationException("Node not found.");
            }
            return (DagNode<T>)node;
        }

        /// <summary>
        /// Gets the indices of nodes that depend on the specified node index.
        /// </summary>
        private IEnumerable<int> GetDependentNodes(int index) => _nodes.Where(n => ((dynamic)n.Value).Dependencies.Contains(index)).Select(n => n.Key);

        /// <summary>
        /// Returns all nodes in the DAG.
        /// </summary>
        public IEnumerable<object> GetAllNodes() => _nodes.Values;

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
        public async Task<T> GetResult<T>(Cell<T> cell)
        {
            if (!_nodes.TryGetValue(cell.Index, out var node)) throw new InvalidOperationException("Node not found.");
            var dagNode = (DagNode<T>)node;
            // Check for reentrancy before accessing the lazy value
            var isComputingField = typeof(DagNodeBase<T>).GetField("_isComputing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isComputingField != null && (bool)isComputingField.GetValue(dagNode))
            {
                // Build dependency chain for debugging
                string chain = BuildDependencyChain(cell.Index);
                throw new InvalidOperationException($"Reentrancy detected: node {cell.Index} is currently being computed. Dependency chain: {chain}");
            }
            var result = await dagNode.DeferredComputedNodeValue.Value;
            return (T)result;
        }

        private string BuildDependencyChain(int startIndex)
        {
            var chain = new List<int> { startIndex };
            var current = startIndex;
            var visited = new HashSet<int> { startIndex };
            while (true)
            {
                var node = _nodes[current];
                var deps = ((dynamic)node).Dependencies as HashSet<int>;
                if (deps == null || deps.Count == 0) break;
                var next = deps.FirstOrDefault(d => !visited.Contains(d));
                if (next == 0 || visited.Contains(next)) break;
                chain.Add(next);
                visited.Add(next);
                current = next;
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
            if (!_nodes.TryGetValue(cell.Index, out var node)) throw new InvalidOperationException("Node not found.");
            var dagNode = (DagNode<T>)node;
            var channel = Channel.CreateUnbounded<T>();
            async void Handler()
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    var newValue = await GetResult(cell);
                    if (!channel.Writer.TryWrite(newValue) && !cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine($"Error writing: {newValue}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            dagNode.NodeUpdated += Handler;

            try
            {
                await foreach (var value in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    yield return value;
                }
            }
            finally
            {
                dagNode.NodeUpdated -= Handler;
                if (!cancellationToken.IsCancellationRequested)
                {
                    channel.Writer.TryComplete();
                }
            }
        }

        /// <summary>
        /// Removes a node from the DAG and cleans up its dependencies.
        /// </summary>
        /// <param name="cell">The cell whose node is to be removed.</param>
        public void RemoveNode<T>(Cell<T> cell)
        {
            if (_nodes.TryRemove(cell.Index, out var node))
            {
                ((DagNode<T>)node).DisposeSubscriptions();
                foreach (var dependentIndex in GetDependentNodes(cell.Index))
                {
                    ((dynamic)_nodes[dependentIndex]).Dependencies.Remove(cell.Index);
                }
                foreach (var dependentIndex in GetDependentNodes(cell.Index))
                {
                    var dependentNode = _nodes[dependentIndex];
                    ((dynamic)dependentNode).DeferredComputedNodeValue = new Lazy<Task<object>>(((dynamic)dependentNode).ComputeNodeValueAsync, LazyThreadSafetyMode.ExecutionAndPublication);
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
            var cell = Cell<TResult>.CreateFunctionCell(_nextIndex++);
            // Check for direct self-dependency and cycles before node creation
            foreach (var c in inputCells)
            {
                if (c.Index == cell.Index)
                    throw new InvalidOperationException("A node cannot depend on itself (Lazy reentrancy protection).");
                // check for cycles from dependency to new node only if new node exists
                if (_nodes.ContainsKey(cell.Index) && IsCyclic(c.Index, cell.Index))
                    throw new InvalidOperationException("Cyclic dependency detected.");
            }
            var node = new DagNode<TResult>(cell, async () =>
            {
                // Prevent recursive lazy evaluation by checking for reentrancy in dependencies
                foreach (var dep in inputCells)
                {
                    var depNode = _nodes[dep.Index] as DagNodeBase<TInputs>;
                    if (depNode != null)
                    {
                        var isComputingField = depNode.GetType().GetField("_isComputing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (isComputingField != null && (bool)isComputingField.GetValue(depNode))
                        {
                            throw new InvalidOperationException($"Reentrancy detected in dependency node {dep.Index} while computing node {cell.Index}.");
                        }
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
                ((DagNode<TResult>)node).Dependencies.Add(c.Index);
            }

            var dependencyCells = inputCells.OfType<BaseCell>();
            ((DagNode<TResult>)node).ConnectDependencies(dependencyCells, ((DagNode<TResult>)node).ComputeNodeValueAsync);

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
            node.ConnectDependencies(dependencies, node.ComputeNodeValueAsync);
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
                foreach (var dependency in ((dynamic)_nodes[current]).Dependencies)
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
                var node = (DagNode<T>)_nodes[cell.Index];
                node.DeferredComputedNodeValue = new Lazy<Task<T>>(() => Task.FromResult(value), LazyThreadSafetyMode.ExecutionAndPublication);
                node.NotifyUpdatedNode();
                await UpdateAndRefresh(cell.Index);
            }
        }

        /// <summary>
        /// Updates and refreshes the nodes based on the provided mode.
        /// </summary>
        /// <param name="startIndex">The starting node index for the update.</param>
        /// <param name="mode">The update mode (update or refresh dependencies).</param>
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
                    var node = _nodes[index];
                    var dependentNodes = GetDependentNodes(index).ToList();
                    await ((dynamic)node).DeferredComputedNodeValue.Value;
                    foreach (var dependentNodeIndex in dependentNodes)
                    {
                        var dependentNode = _nodes[dependentNodeIndex];
                        // Use correct type for Lazy<Task<T>> for function nodes
                        var nodeType = dependentNode.GetType();
                        var cellProp = nodeType.GetProperty("Cell");
                        var cellObj = cellProp?.GetValue(dependentNode);
                        var cellType = cellObj?.GetType();
                        var valueType = cellType?.GenericTypeArguments.FirstOrDefault();
                        if (valueType != null)
                        {
                            var computeMethod = nodeType.GetMethod("ComputeNodeValueAsync");
                            var lazyType = typeof(Lazy<>).MakeGenericType(typeof(Task<>).MakeGenericType(valueType));
                            var ctor = lazyType.GetConstructor(new[] { typeof(Func<>).MakeGenericType(typeof(Task<>).MakeGenericType(valueType)), typeof(LazyThreadSafetyMode) });
                            var funcType = typeof(Func<>).MakeGenericType(typeof(Task<>).MakeGenericType(valueType));
                            var func = Delegate.CreateDelegate(funcType, dependentNode, computeMethod);
                            var newLazy = ctor.Invoke(new object[] { func, LazyThreadSafetyMode.ExecutionAndPublication });
                            var lazyProp = nodeType.GetProperty("DeferredComputedNodeValue");
                            lazyProp?.SetValue(dependentNode, newLazy);
                        }
                        else
                        {
                            // fallback for if nodes are  non-generic 
                            ((dynamic)dependentNode).DeferredComputedNodeValue = new Lazy<Task<object>>(((dynamic)dependentNode).ComputeNodeValueAsync, LazyThreadSafetyMode.ExecutionAndPublication);
                        }
                        stack.Push(dependentNodeIndex);
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Serializes the current DAG structure (nodes, values, dependencies, types) to a JSON string.
        /// NB: Custom functions cannot be serialized and de-serialized so for now we just serialize structure.
        /// </summary>
        public string ToJson()
        {
            var nodeDtos = _nodes.Values.Select(node => new
            {
                Index = ((dynamic)node).Cell.Index,
                Type = ((dynamic)node).Cell.CellType.ToString(),
                Value = ((dynamic)node).Cell.Value,
                Dependencies = ((IEnumerable<int>)((dynamic)node).Dependencies).ToArray()
            }).ToList();
            return JsonConvert.SerializeObject(nodeDtos, Formatting.Indented);
        }        
    }
}