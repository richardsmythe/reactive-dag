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
        private readonly ConcurrentDictionary<int, IDagNodeOperations> _nodes = new ConcurrentDictionary<int, IDagNodeOperations>();
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
        private IEnumerable<int> GetDependentNodes(int index) => 
            _nodes.Where(n => n.Value.GetDependencies().Contains(index))
                  .Select(n => n.Key);

        /// <summary>
        /// Returns all nodes in the DAG.
        /// </summary>
        public IEnumerable<IDagNodeOperations> GetAllNodes() => _nodes.Values;

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
            
            // Check for reentrancy
            if (((DagNodeBase<T>)node).IsComputing())
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
            if (!_nodes.TryGetValue(cell.Index, out var node)) 
                throw new InvalidOperationException("Node not found.");
            
            var dagNode = (DagNode<T>)node;
            var channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
            
            async void Handler()
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    
                    var newValue = await GetResult(cell);
                    if (!channel.Writer.TryWrite(newValue) && !cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine($"Error writing to channel: {newValue}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in NodeUpdated handler: {ex.Message}");
                }
            }

            // Subscribe to the node's update event
            dagNode.NodeUpdated += Handler;

            try
            {
                // Get initial value to start the stream
                var initialValue = await GetResult(cell);
                await channel.Writer.WriteAsync(initialValue, cancellationToken);
                
                // Read from the channel and yield results
                await foreach (var value in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    yield return value;
                }
            }
            finally
            {
                // Clean up event handler and channel
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
                
                // Store dependent nodes before removing the node
                var dependentIndices = GetDependentNodes(cell.Index).ToList();
                
                // Remove dependency from all dependent nodes
                foreach (var dependentIndex in dependentIndices)
                {
                    if (_nodes.TryGetValue(dependentIndex, out var dependentNode))
                    {
                        dependentNode.GetDependencies().Remove(cell.Index);
                    }
                }
                
                // Reset computation for all dependent nodes
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
        /// <param name="value">The value of the input cell.</typeparam>
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
                    if (_nodes.TryGetValue(dep.Index, out var depNode) &&
                        depNode is DagNodeBase<TInputs> typedNode && typedNode.IsComputing())
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
            }

            var dependencyCells = inputCells.OfType<BaseCell>();
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
                
                if (_nodes.TryGetValue(cell.Index, out var node) && node is DagNode<T> typedNode)
                {
                    // Update the node's lazy value with the new input value
                    typedNode.DeferredComputedNodeValue = new Lazy<Task<T>>(() => Task.FromResult(value), LazyThreadSafetyMode.ExecutionAndPublication);
                    
                    // Notify that this node has updated
                    typedNode.NotifyUpdatedNode();
                    
                    // Refresh all dependent nodes
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
                    
                    // Get dependent nodes before evaluating - they depend on the current state
                    var dependentIndices = GetDependentNodes(index).ToList();

                    await node.EvaluateAsync();
                    
                    foreach (var dependentIndex in dependentIndices)
                    {
                        if (_nodes.TryGetValue(dependentIndex, out var dependentNode))
                        {
                            // Reset computation for this node to force reevaluation
                            dependentNode.ResetComputation();
                            
                            // Evaluate the node to update its value
                            await dependentNode.EvaluateAsync();
                            
                            // Trigger a notification for this node
                            NotifyNodeUpdated(dependentNode);
                            
                            // Add this node to the stack to process its dependents
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
        
        /// <summary>
        /// Helper method to notify that a node has been updated, using the appropriate method based on type
        /// </summary>
        private void NotifyNodeUpdated(IDagNodeOperations node)
        {
            // Use a type-specific approach based on the actual node type
            if (node is DagNode<int> intNode)
            {
                intNode.NotifyUpdatedNode();
            }
            else if (node is DagNode<double> doubleNode)
            {
                doubleNode.NotifyUpdatedNode();
            } 
            else if (node is DagNode<string> stringNode)
            {
                stringNode.NotifyUpdatedNode();
            }
            else if (node is DagNode<bool> boolNode)
            {
                boolNode.NotifyUpdatedNode();
            }
            else if (node is DagNode<object> objNode)
            {
                objNode.NotifyUpdatedNode();
            }
            else
            {
                // If we can't determine the exact type, try using reflection as a fallback
                var nodeType = node.GetType();
                var notifyMethod = nodeType.GetMethod("NotifyUpdatedNode", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                notifyMethod?.Invoke(node, null);
            }
        }

        /// <summary>
        /// Serializes the current DAG structure (nodes, values, dependencies, types) to a JSON string.
        /// NB: Custom functions cannot be serialized and de-serialized so for now we just serialize structure.
        /// </summary>
        public string ToJson()
        {
            var nodeDtos = _nodes.Values.Select(node => {
                var cell = node.GetCell();
                return new
                {
                    Index = cell.Index,
                    Type = cell.CellType.ToString(),
                    Value = GetCellValue(cell),
                    Dependencies = node.GetDependencies().ToArray()
                };
            }).ToList();
            
            return JsonConvert.SerializeObject(nodeDtos, Formatting.Indented);
        }
        
        private object GetCellValue(BaseCell cell)
        {
            // Use reflection to get the Value property from the cell
            var type = cell.GetType();
            var valueProp = type.GetProperty("Value");
            return valueProp?.GetValue(cell);
        }
    }
}