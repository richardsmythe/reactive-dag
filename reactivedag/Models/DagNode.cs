using ReactiveDAG.Core.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;

public class DagNode<T> : DagNodeBase<T>
{
    private readonly Func<Task<T>> _computeNodeValue;
    private readonly SemaphoreSlim _computeLock = new SemaphoreSlim(1, 1);
    private T _lastComputedValueCache;

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

        OnNodeUpdated();
    }

    public override async Task<T> ComputeNodeValueAsync()
    {
        bool valueChanged = false;
        await _computeLock.WaitAsync();
        try
        {
            UpdateStatus(NodeStatus.Processing);
            var newValue = await _computeNodeValue();

            if (EqualityComparer<T>.Default.Equals(_lastComputedValueCache, newValue))
            {
                UpdateStatus(NodeStatus.Completed);
                return _lastComputedValueCache;
            }

            _lastComputedValueCache = newValue;

            if (Cell is Cell<T> reactiveCell)
            {
                reactiveCell.Value = newValue;
            }

            valueChanged = true;
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

        // Intentionally unreachable due to return/throw above — 
        // notification is handled by the engine's UpdateAndRefresh flow
    }
    
    public override void ResetComputation()
    {
        DeferredComputedNodeValue = new Lazy<Task<T>>(_computeNodeValue, LazyThreadSafetyMode.ExecutionAndPublication);
    }
}
