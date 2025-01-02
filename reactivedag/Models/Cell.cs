namespace ReactiveDAG.Core.Models
{
    public class Cell<T> : BaseCell
    {
        public T Value { get; set; }
        public T PreviousValue { get; set; }
        public Action<T> OnValueChanged { get; set; }

        public Cell(int index, CellType type, T value)
        {
            Index = index;
            CellType = type;
            Value = value;
            PreviousValue = value;
        }

        public static Cell<T> CreateInputCell(int index, T value) => new Cell<T>(index, CellType.Input, value);
        public static Cell<T> CreateFunctionCell(int index) => new Cell<T>(index, CellType.Function, default);
        public override string GetValueTypeName() => typeof(T).Name;
    }

    public enum CellType
    {
        Input,
        Function
    }
}