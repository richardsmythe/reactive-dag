using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveDAG.Core.Models
{
    public abstract class DagNodeBase<T> : IDagNodeOperations
    {
        public BaseCell Cell { get; set; }
        public HashSet<int> Dependencies { get; set; } = new();
        public Lazy<Task<T>> DeferredComputedNodeValue { get; set; }
        public List<IDisposable> Subscriptions { get; set; }
        public NodeStatus Status { get; protected set; } = NodeStatus.Idle;
        protected bool _isComputing = false;
        private int _pendingRecompute; // how many dependency updates still need to be processed, when it goes from 0 to 1 it tells us a worker must run
        private readonly Dictionary<int, IDisposable> _dependencySubscriptions = new(); // tracks dependency subscriptions for cleanup
        public event Action NodeUpdated;

        /// <summary>
        /// Creates a node wrapper around a cell and the function that knows how to compute its value.
        /// </summary>
        protected DagNodeBase(
            BaseCell cell,
            Func<Task<T>> computeNodeValue)
        {
            Cell = cell;
            DeferredComputedNodeValue = new Lazy<Task<T>>(async () =>
            {
                if (_isComputing)
                    throw new InvalidOperationException($"Reentrancy detected: node {Cell.Index} is currently being computed. Dependency chain: {string.Join(" -> ", Dependencies)}");
                _isComputing = true;
                try
                {
             
                    return await computeNodeValue();
                }
                finally
                {
                    _isComputing = false;
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication);
            Subscriptions = new List<IDisposable>();
        }

        /// <summary>
        /// Concrete nodes implement this to actually produce their value.
        /// </summary>
        public abstract Task<T> ComputeNodeValueAsync();

        /// <summary>
        /// Updates the exposed status flag.
        /// </summary>
        protected void UpdateStatus(NodeStatus newStatus) => Status = newStatus;

        /// <summary>
        /// Triggers the NodeUpdated event
        /// </summary>
        protected void OnNodeUpdated()
        {
            NodeUpdated?.Invoke();
        }

        /// <summary>
        /// Subscribes to dependency cells and wires them into the computation pipeline.
        /// </summary>
        public void ConnectDependencies(IEnumerable<BaseCell> dependencyCells, Func<Task<T>> computeNodeValue)
        {
            foreach (var dependency in dependencyCells)
            {
                var index = dependency.Index;
        
                if (_dependencySubscriptions.TryGetValue(index, out var existing))
                {
                    existing.Dispose();
                    _dependencySubscriptions.Remove(index);
                }

                var subscription = dependency.Subscribe(_ => ScheduleRecompute());
                _dependencySubscriptions[index] = subscription;
                Subscriptions.Add(subscription);
            }
            DeferredComputedNodeValue = new Lazy<Task<T>>(async () =>
            {
                if (_isComputing)
                    throw new InvalidOperationException($"Reentrancy detected: node {Cell.Index} is currently being computed. Dependency chain: {string.Join(" -> ", Dependencies)}");
                _isComputing = true;
                try
                {
            
                    return await computeNodeValue();
                }
                finally
                {
                    _isComputing = false;
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Schedules (or rejoins) a background recomputation when dependencies change.
        /// </summary>
        private void ScheduleRecompute()
        {

            if (Interlocked.Increment(ref _pendingRecompute) == 1)
            {
                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        var shouldContinue = false;
                        try
                        {
                            await ComputeNodeValueAsync().ConfigureAwait(false);
                        }
                        finally
                        {
                            shouldContinue = Interlocked.Decrement(ref _pendingRecompute) > 0;
                        }

                        if (!shouldContinue)
                        {
                            break;
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Disposes every dependency subscription tracked by this node.
        /// </summary>
        public void DisposeSubscriptions()
        {
            foreach (var subscription in Subscriptions)
            {
                subscription.Dispose();
            }
            Subscriptions.Clear();
            foreach (var kvp in _dependencySubscriptions)
            {
                kvp.Value.Dispose();
            }
            _dependencySubscriptions.Clear();
        }


        /// <summary>
        /// Returns the dependency ids tracked by this node.
        /// </summary>
        public HashSet<int> GetDependencies() => Dependencies;

        /// <summary>
        /// Returns the wrapped cell.
        /// </summary>
        public BaseCell GetCell() => Cell;

        /// <summary>
        /// Allows derived nodes to reset their cached computation state.
        /// </summary>
        public abstract void ResetComputation();

        /// <summary>
        /// Evaluates the node and returns the value boxed as an object.
        /// </summary>
        public virtual async Task<object> EvaluateAsync()
        {
            return await DeferredComputedNodeValue.Value;
        }


        /// <summary>
        /// Indicates whether the node is currently computing.
        /// </summary>
        public bool IsComputing() => _isComputing;

        /// <summary>
        /// Raises the NodeUpdated event.
        /// </summary>
        public void NotifyUpdatedNode() => OnNodeUpdated();

        /// <summary>
        /// Removes a specific dependency and disposes its subscription.
        /// </summary>
        public void RemoveDependency(int dependencyIndex)
        {
            Dependencies.Remove(dependencyIndex);
            if (_dependencySubscriptions.TryGetValue(dependencyIndex, out var sub))
            {
                sub.Dispose();
                _dependencySubscriptions.Remove(dependencyIndex);
                Subscriptions.Remove(sub);
            }
        }
    }
}