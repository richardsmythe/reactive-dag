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
        object result;
        await _computeLock.WaitAsync();
        try
        {
            result = await _computeNodeValue();

            if (Cell is Cell<object> reactiveCell)
            {
                reactiveCell.Value = result;
            }
        }
        finally
        {
            _computeLock.Release();
        }

        _ = Task.Run(() => NotifyUpdatedNode()); // need this so that the notification can happen asynchronously
        return result;
    }


}
