using ReactiveDAG.Core.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;

public class DagNode<T> : DagNodeBase<T>
{
    private readonly Func<Task<T>> _computeNodeValue;
    private readonly SemaphoreSlim _computeLock = new SemaphoreSlim(1, 1);
    private T _lastComputedValueCache;
    private readonly BehaviorSubject<NodeStatus> _statusSubject = new(NodeStatus.Idle);
    public event Action NodeUpdated;

    public DagNode(Cell<T> cell, Func<Task<T>> computeValue)
        : base(cell, computeValue)
    {
        _computeNodeValue = computeValue;
    }

    public T GetCellValue()
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

    public override async Task<T> ComputeNodeValueAsync()
    {
        await _computeLock.WaitAsync();
        try
        {
            UpdateStatus(NodeStatus.Processing);
            var newValue = await _computeNodeValue();

            if (_lastComputedValueCache != null && _lastComputedValueCache.Equals(newValue))
            {
                return _lastComputedValueCache;
            }

            _lastComputedValueCache = newValue;

            if (Cell is Cell<T> reactiveCell)
            {
                reactiveCell.Value = newValue;
            }

            _ = Task.Run(() => NotifyUpdatedNode());
            
            UpdateStatus(NodeStatus.Completed);
            return newValue;
        }
        catch (Exception)
        {
            UpdateStatus(NodeStatus.Failed);
            throw;
        }
        finally
        {
            _computeLock.Release();
        }
    }
}
