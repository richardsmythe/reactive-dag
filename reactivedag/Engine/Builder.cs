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
        /// <typeparam name="TInputs">The type of the input values.</typeparam>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="function">The function to be executed, taking an array of TInputs as input.</param>
        /// <param name="resultCell">The output cell containing the function's result.</param>
        /// <returns>The current <see cref="Builder"/> instance for method chaining.</returns>
        public DagPipelineBuilder AddFunction<TInputs, TResult>(Func<TInputs[], Task<TResult>> function, out Cell<TResult> resultCell)
        {
            try
            {
                var inputCells = _cells.Cast<Cell<TInputs>>().ToArray();
                resultCell = _dagEngine.AddFunction<TInputs, TResult>(inputCells, function);
                _cells.Clear();
                _cells.Add(resultCell);
                return this;
            }
            catch (InvalidCastException ex)
            {
                throw new InvalidOperationException($"Type mismatch in AddFunction: expected Cell<{typeof(TInputs).Name}> but found a different type. Ensure AddInput and AddFunction use the same type.", ex);
            }
        }

        /// <summary>
        /// Adds a function node to the DAG that computes a result based on its input cells.
        /// </summary>
        /// <typeparam name="TInputs">The type of the input values.</typeparam>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="function">The function to be executed, taking an array of TInputs as input.</param>
        /// <returns>The current <see cref="Builder"/> instance for method chaining.</returns>
        public DagPipelineBuilder AddFunction<TInputs, TResult>(Func<TInputs[], Task<TResult>> function)
        {
            var inputCells = _cells.Cast<Cell<TInputs>>().ToArray();
            var cell = _dagEngine.AddFunction<TInputs, TResult>(inputCells, function);
            _cells.Clear();
            _cells.Add(cell);
            return this;
        }

        /// <summary>
        /// Adds a function node to the DAG that computes a result based on an explicit set of dependency cells.
        /// </summary>
        /// <typeparam name="TInputs">The type of the input values.</typeparam>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="dependencies">The explicit dependency cells for this function node.</param>
        /// <param name="function">The function to be executed, taking an array of TInputs as input.</param>
        /// <param name="resultCell">The output cell containing the function's result.</param>
        /// <returns>The current <see cref="Builder"/> instance for method chaining.</returns>
        public DagPipelineBuilder AddFunction<TInputs, TResult>(Cell<TInputs>[] dependencies, Func<TInputs[], Task<TResult>> function, out Cell<TResult> resultCell)
        {
            resultCell = _dagEngine.AddFunction<TInputs, TResult>(dependencies, function);
            _cells.Clear();
            _cells.Add(resultCell);
            return this;
        }

        /// <summary>
        /// Adds a function node to the DAG that computes a result based on a variable number of dependency cells.
        /// </summary>
        /// <typeparam name="TInputs">The type of the input values.</typeparam>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="function">The function to be executed, taking an array of TInputs as input.</param>
        /// <param name="resultCell">The output cell containing the function's result.</param>
        /// <param name="dependencies">The dependency cells for this function node.</param>
        /// <returns>The current <see cref="DagPipelineBuilder"/> instance for method chaining.</returns>
        public DagPipelineBuilder AddFunction<TInputs, TResult>(
            Func<TInputs[], Task<TResult>> function,
            out Cell<TResult> resultCell,
            params Cell<TInputs>[] dependencies)
        {
            resultCell = _dagEngine.AddFunction<TInputs, TResult>(dependencies, function);
            _cells.Clear();
            _cells.Add(resultCell);
            return this;
        }

        /// <summary>
        /// Adds a function node to the DAG that computes a result based on an explicit set of dependency cells of any type.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="dependencies">The explicit dependency cells for this function node.</param>
        /// <param name="function">The function to be executed, taking an array of objects as input.</param>
        /// <param name="resultCell">The output cell containing the function's result.</param>
        /// <returns>The current <see cref="Builder"/> instance for method chaining.</returns>
        public DagPipelineBuilder AddFunction<TResult>(BaseCell[] dependencies, Func<object[], Task<TResult>> function, out Cell<TResult> resultCell)
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
        /// <param name="newValue">The new value to set.</typeparam>
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
            // Try to cast to Cell<object> for removal
            if (cell is Cell<object> objectCell)
                _dagEngine.RemoveNode(objectCell);
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
            // Try to cast to Cell<T> for result
            if (cell is Cell<T> typedCell)
                return await _dagEngine.GetResult<T>(typedCell);
            throw new InvalidCastException($"Cell is not of type Cell<{typeof(T).Name}>");
        }

        /// <summary>
        /// Combines any number of cells into a single function cell whose value is an object array containing the values of the input cells.
        /// </summary>
        /// <param name="cells">The cells to combine into a tuple cell.</param>
        /// <returns>A function cell whose value is an object array of the input cell values.</returns>
        public Cell<object[]> CombineCells(params BaseCell[] cells)
        {
            return _dagEngine.AddFunction(
                cells,
                async inputs => inputs
            );
        }
    }
}
