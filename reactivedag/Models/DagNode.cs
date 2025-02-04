using ReactiveDAG.Core.Models;

public class DagNode : DagNodeBase
{
    private readonly Func<Task<object>> _computeNodeValue;
    private readonly SemaphoreSlim _computeLock = new SemaphoreSlim(1, 1);


    public event Action NodeUpdated;

    public DagNode(BaseCell cell, Func<Task<object>> computeValue)
        : base(cell, computeValue)
    {
        _computeNodeValue = computeValue;
    }

    public T GetCellValue<T>()
    {
        if (Cell is Cell<T> typedCell)
        {
            return typedCell.Value;
        }
        throw new InvalidOperationException("Cell is not of the expected type.");
    }

    public void NotifyUpdatedNode()
    {
        NodeUpdated?.Invoke();
    }


    public override async Task<object> ComputeNodeValueAsync()
    {
        await _computeLock.WaitAsync(); 

        try
        {
            var result = await _computeNodeValue(); 

            if (Cell is Cell<object> reactiveCell)
            {
                reactiveCell.Value = result;
            }
            NotifyUpdatedNode();
            return result;
        }
        finally
        {
            _computeLock.Release();
        }
    }

}
