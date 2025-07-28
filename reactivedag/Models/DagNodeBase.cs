namespace ReactiveDAG.Core.Models
{
    public abstract class DagNodeBase<T>
    {
        public BaseCell Cell { get; set; }
        public HashSet<int> Dependencies { get; set; } = new();
        public Lazy<Task<T>> DeferredComputedNodeValue { get; set; }
        public List<IDisposable> Subscriptions { get; set; }
        public NodeStatus Status { get; protected set; } = NodeStatus.Idle;
        private bool _isComputing = false;

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
    }
}