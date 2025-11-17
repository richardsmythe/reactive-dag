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

        // Track per-dependency subscriptions for precise cleanup
        private readonly Dictionary<int, IDisposable> _dependencySubscriptions = new();

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
                var index = dependency.Index;
        
                if (_dependencySubscriptions.TryGetValue(index, out var existing))
                {
                    existing.Dispose();
                    _dependencySubscriptions.Remove(index);
                }

                var subscription = dependency.Subscribe(value =>
                {
                    if (!_isComputing)
                        Task.Run(() => ComputeNodeValueAsync());
                });
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


        public HashSet<int> GetDependencies() => Dependencies;

        public BaseCell GetCell() => Cell;

        public abstract void ResetComputation();

        public virtual async Task<object> EvaluateAsync()
        {
            return await DeferredComputedNodeValue.Value;
        }


        public bool IsComputing() => _isComputing;

        public void NotifyUpdatedNode() => OnNodeUpdated();

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