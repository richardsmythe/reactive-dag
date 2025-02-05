using System;
using System.Runtime.Intrinsics.Arm;

namespace ReactiveDAG.Core.Models
{
    public abstract class DagNodeBase
    {
        public BaseCell Cell { get; set; }
        public HashSet<int> Dependencies { get; set; } = new();
        public Lazy<Task<object>> DeferredComputedNodeValue { get; set; }
        public List<IDisposable> Subscriptions { get; set; }

        protected DagNodeBase(
            BaseCell cell,
            Func<Task<object>> computeNodeValue)
        {
            Cell = cell;
            DeferredComputedNodeValue = new Lazy<Task<object>>(computeNodeValue, LazyThreadSafetyMode.ExecutionAndPublication);
            Subscriptions = new List<IDisposable>();
        }

        public abstract Task<object> ComputeNodeValueAsync();

        public void ConnectDependencies(IEnumerable<BaseCell> dependencyCells, Func<Task<object>> computeNodeValue)
        {
            foreach (var dependency in dependencyCells)
            {
                var subscription = dependency.Subscribe(async value =>
                {                   
                    await ComputeNodeValueAsync();
                });
                Subscriptions.Add(subscription);
            }
            DeferredComputedNodeValue = new Lazy<Task<object>>(computeNodeValue, LazyThreadSafetyMode.ExecutionAndPublication);
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