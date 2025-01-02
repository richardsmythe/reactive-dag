namespace ReactiveDAG.Core.Models
{
    public class DagNode : DagNodeBase
    {
        private readonly Func<Task<object>> _computeNodeValue;

        public DagNode(BaseCell cell, Func<Task<object>> computeValue)
            : base(cell, computeValue)
        {
            _computeNodeValue = computeValue;
        }

        public override async Task<object> ComputeNodeValueAsync()
        {
            try
            {
                return await _computeNodeValue();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error computing node value.", ex);
            }
        }
    }
}