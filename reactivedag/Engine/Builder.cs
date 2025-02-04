using ReactiveDAG.Core.Models;

namespace ReactiveDAG.Core.Engine
{
    /// <summary>
    /// A builder class for constructing a Directed Acyclic Graph (DAG) using the <see cref="DagEngine"/>.
    /// </summary>
    public class Builder
    {
        private readonly DagEngine _dagEngine;
        private readonly List<BaseCell> _cells = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="Builder"/> class.
        /// </summary>
        public Builder()
        {
            _dagEngine = new DagEngine();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Builder"/> class.
        /// </summary>
        /// <returns>A new <see cref="Builder"/> instance.</returns>
        public static Builder Create()
        {
            return new Builder();
        }

        /// <summary>
        /// Adds an input value to the DAG and returns the created cell.
        /// </summary>
        /// <typeparam name="T">The type of the input value.</typeparam>
        /// <param name="value">The input value.</param>
        /// <param name="cell">The created input cell.</param>
        /// <returns>The current <see cref="Builder"/> instance for method chaining.</returns>
        public Builder AddInput<T>(T value, out Cell<T> cell)
        {
            cell = _dagEngine.AddInput(value);
            _cells.Add(cell);
            return this;
        }

        /// <summary>
        /// Adds an input value to the DAG.
        /// </summary>
        /// <typeparam name="T">The type of the input value.</typeparam>
        /// <param name="value">The input value.</param>
        /// <returns>The current <see cref="Builder"/> instance for method chaining.</returns>
        public Builder AddInput<T>(T value)
        {
            var cell = _dagEngine.AddInput(value);
            _cells.Add(cell);
            return this;
        }

        /// <summary>
        /// Adds a function node to the DAG that computes a result based on its input cells.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="function">The function to be executed, taking an array of objects as input.</param>
        /// <param name="resultCell">The output cell containing the function's result.</param>
        /// <returns>The current <see cref="Builder"/> instance for method chaining.</returns>
        public Builder AddFunction<TResult>(Func<object[], TResult> function, out Cell<TResult> resultCell)
        {
            resultCell = _dagEngine.AddFunction(_cells.ToArray(), function);
            return this;
        }

        /// <summary>
        /// Adds a function node to the DAG that computes a result based on its input cells.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="function">The function to be executed, taking an array of objects as input.</param>
        /// <returns>The current <see cref="Builder"/> instance for method chaining.</returns>
        public Builder AddFunction<TResult>(Func<object[], TResult> function)
        {
            _dagEngine.AddFunction(_cells.ToArray(), function);
            return this;
        }

        /// <summary>
        /// Updates an existing input cell in the DAG with a new value.
        /// </summary>
        /// <typeparam name="T">The type of the input value.</typeparam>
        /// <param name="cell">The input cell to update.</param>
        /// <param name="newValue">The new value to set.</param>
        /// <returns>The current <see cref="Builder"/> instance for method chaining.</returns>
        public Builder UpdateInput<T>(Cell<T> cell, T newValue)
        {
            _dagEngine.UpdateInput(cell, newValue);
            return this;
        }

        /// <summary>
        /// Builds and returns the constructed <see cref="DagEngine"/>.
        /// </summary>
        /// <returns>The constructed <see cref="DagEngine"/> instance.</returns>
        public DagEngine Build()
        {
            return _dagEngine;
        }
    }
}
