namespace ReactiveDAG.Core.Models
{
    public abstract class DagNodeBase
    {
        public BaseCell Cell { get; set; }
        public HashSet<int> Dependencies { get; set; } = new();
        public Lazy<Task<object>> DeferredComputedNodeValue { get; set; }

        protected DagNodeBase(
            BaseCell cell,
            Func<Task<object>> computeNodeValue)
        {
            Cell = cell;
            DeferredComputedNodeValue = new Lazy<Task<object>>(computeNodeValue);
        }

        public abstract Task<object> ComputeNodeValueAsync();
    }
}