using ReactiveDAG.Core.Models;

namespace ReactiveDAG.Core.Engine
{
    public class Builder
    {
        private readonly DagEngine _dagEngine;
        private readonly List<BaseCell> _cells = [];
        public Builder()
        {
            _dagEngine = new DagEngine();
        }

        public static Builder Create()
        {
            return new Builder();
        }

        public Builder AddInput<T>(T value, out Cell<T> cell)
        {
            cell = _dagEngine.AddInput(value);
            _cells.Add(cell);
            return this;
        }

        public Builder AddInput<T>(T value)
        {
            var cell = _dagEngine.AddInput(value);
            _cells.Add(cell);
            return this;
        }

        public Builder AddFunction<TResult>(Func<object[], TResult> function, out Cell<TResult> resultCell)
        {
            resultCell = _dagEngine.AddFunction(_cells.ToArray(), function);
            return this;
        }

        public Builder AddFunction<TResult>(Func<object[], TResult> function)
        {
            _dagEngine.AddFunction(_cells.ToArray(), function);
            return this;
        }

        public Builder UpdateInput<T>(Cell<T> cell, T newValue)
        {
            _dagEngine.UpdateInput(cell, newValue);
            return this;
        }

        public DagEngine Build()
        {
            return _dagEngine;
        }
    }

}
