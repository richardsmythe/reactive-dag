namespace ReactiveDAG.Core.Models;

public abstract class BaseCell
{
    public int Index { get; set; }
    public CellType CellType { get; set; }
    public abstract string GetValueTypeName();
}