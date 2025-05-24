using ReactiveDAG.Core.Models;

namespace ReactiveDAG.Core.Engine
{
    /// <summary>
    /// A builder class for constructing a Directed Acyclic Graph (DAG) using the <see cref="DagEngine"/>.
    /// </summary>
    public class DagPipelineBuilder
    {
        private readonly DagEngine _dagEngine;
        private readonly List<BaseCell> _cells = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="Builder"/> class.
        /// </summary>
        public DagPipelineBuilder()
        {
            _dagEngine = new DagEngine();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Builder"/> class.
        /// </summary>
        /// <returns>A new <see cref="Builder"/> instance.</returns>
        public static DagPipelineBuilder Create()
        {
            return new DagPipelineBuilder();
        }

        /// <summary>
        /// Adds an input value to the DAG and returns the created cell.
        /// </summary>
        /// <typeparam name="T">The type of the input value.</typeparam>
        /// <param name="value">The input value.</param>
        /// <param name="cell">The created input cell.</param>
        /// <returns>The current <see cref="Builder"/> instance for method chaining.</returns>
        public DagPipelineBuilder AddInput<T>(T value, out Cell<T> cell)
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
        public DagPipelineBuilder AddInput<T>(T value)
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
        public DagPipelineBuilder AddFunction<TResult>(Func<object[], Task<TResult>> function, out Cell<TResult> resultCell)
        {
            resultCell = _dagEngine.AddFunction(_cells.ToArray(), function);
            _cells.Clear();
            _cells.Add(resultCell);
            return this;
        }

        /// <summary>
        /// Adds a function node to the DAG that computes a result based on its input cells.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="function">The function to be executed, taking an array of objects as input.</param>
        /// <returns>The current <see cref="Builder"/> instance for method chaining.</returns>
        public DagPipelineBuilder AddFunction<TResult>(Func<object[], Task<TResult>> function)
        {
            var cell = _dagEngine.AddFunction(_cells.ToArray(), function);
            _cells.Clear();
            _cells.Add(cell);
            return this;
        }

        /// <summary>
        /// Adds a function node to the DAG that computes a result based on an explicit set of dependency cells.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="dependencies">The explicit dependency cells for this function node.</param>
        /// <param name="function">The function to be executed, taking an array of objects as input.</param>
        /// <param name="resultCell">The output cell containing the function's result.</param>
        /// <returns>The current <see cref="Builder"/> instance for method chaining.</returns>
        public DagPipelineBuilder AddFunction<TResult>(BaseCell[] dependencies, Func<object[], Task<TResult>> function, out Cell<TResult> resultCell)
        {
            resultCell = _dagEngine.AddFunction(dependencies, function);
            // Optionally, clear _cells if you want to use only the new cell for subsequent nodes.
            _cells.Clear();
            _cells.Add(resultCell);
            return this;
        }

        /// <summary>
        /// Adds a function node to the DAG that computes a result based on a variable number of dependency cells.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="function">The function to be executed, taking an array of objects as input.</param>
        /// <param name="resultCell">The output cell containing the function's result.</param>
        /// <param name="dependencies">The dependency cells for this function node.</param>
        /// <returns>The current <see cref="DagPipelineBuilder"/> instance for method chaining.</returns>
        public DagPipelineBuilder AddFunction<TResult>(
            Func<object[], Task<TResult>> function,
            out Cell<TResult> resultCell,
            params BaseCell[] dependencies)
        {
            resultCell = _dagEngine.AddFunction(dependencies, function);
            _cells.Clear();
            _cells.Add(resultCell);
            return this;
        }

        /// <summary>
        /// Updates an existing input cell in the DAG with a new value.
        /// </summary>
        /// <typeparam name="T">The type of the input value.</typeparam>
        /// <param name="cell">The input cell to update.</param>
        /// <param name="newValue">The new value to set.</param>
        /// <returns>The current <see cref="Builder"/> instance for method chaining.</returns>
        public DagPipelineBuilder UpdateInput<T>(Cell<T> cell, T newValue)
        {
            _dagEngine.UpdateInput(cell, newValue);
            return this;
        }

        /// <summary>
        /// Removes a node from the DAG and updates the builder's cell list.
        /// </summary>
        /// <param name="cell">The cell to remove.</param>
        /// <returns>The current <see cref="DagPipelineBuilder"/> instance for method chaining.</returns>
        public DagPipelineBuilder RemoveNode(BaseCell cell)
        {
            _dagEngine.RemoveNode(cell);
            _cells.Remove(cell);
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

        /// <summary>
        /// Converts the DAG to a JSON representation.
        /// </summary>
        /// <returns>A JSON string representing the DAG.</returns>
        public string ToJson()
        {
            return _dagEngine.ToJson();
        }

        /// <summary>
        /// Gets the result for a specific cell asynchronously using the underlying DagEngine.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="cell">The cell whose result is to be retrieved.</param>
        /// <returns>The result of the cell.</returns>
        public async Task<T> GetResult<T>(BaseCell cell)
        {
            return await _dagEngine.GetResult<T>(cell);
        }
    }
}
