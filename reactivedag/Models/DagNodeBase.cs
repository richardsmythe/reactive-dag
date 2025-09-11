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
        public event Action NodeUpdated;

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
                    // Only call computeNodeValue, never access DeferredComputedNodeValue.Value here
                    return await computeNodeValue();
                }
                finally
                {
                    _isComputing = false;
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication);
            Subscriptions = new List<IDisposable>();
        }

        public abstract Task<T> ComputeNodeValueAsync();

        protected void UpdateStatus(NodeStatus newStatus) => Status = newStatus;

        /// <summary>
        /// Triggers the NodeUpdated event
        /// </summary>
        protected void OnNodeUpdated()
        {
            NodeUpdated?.Invoke();
        }

        public void ConnectDependencies(IEnumerable<BaseCell> dependencyCells, Func<Task<T>> computeNodeValue)
        {
            foreach (var dependency in dependencyCells)
            {
                var subscription = dependency.Subscribe(value =>
                {
                    if (!_isComputing)
                        Task.Run(() => ComputeNodeValueAsync());
                });
                Subscriptions.Add(subscription);
            }
            DeferredComputedNodeValue = new Lazy<Task<T>>(async () =>
            {
                if (_isComputing)
                    throw new InvalidOperationException($"Reentrancy detected: node {Cell.Index} is currently being computed. Dependency chain: {string.Join(" -> ", Dependencies)}");
                _isComputing = true;
                try
                {
                    // Only call computeNodeValue, never access DeferredComputedNodeValue.Value here
                    return await computeNodeValue();
                }
                finally
                {
                    _isComputing = false;
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public void DisposeSubscriptions()
        {
            foreach (var subscription in Subscriptions)
            {
                subscription.Dispose();
            }
            Subscriptions.Clear();
        }

        // IDagNodeOperations implementation
        public HashSet<int> GetDependencies() => Dependencies;

        public BaseCell GetCell() => Cell;

        public abstract void ResetComputation();

        public virtual async Task<object> EvaluateAsync()
        {
            return await DeferredComputedNodeValue.Value;
        }

        // Helper methods
        public bool IsComputing() => _isComputing;
    }
}