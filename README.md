
# Reactive Directed Acyclic Graph (ReactiveDAG) Project

## Overview

The **ReactiveDAG** project provides a flexible and scalable framework for managing **reactive dependencies** using a **Directed Acyclic Graph (DAG)**. It allows developers to define a set of inputs and functions, track dependencies between them, and efficiently propagate changes throughout the system.

### Key Components

1. **DagEngine**:  
   The core engine responsible for managing nodes, updating values, and propagating changes. It provides functionality for:
   - Adding inputs and functions as nodes.
   - Handling cyclic dependency detection.
   - Propagating updates and refreshing dependencies.
   - Ensuring thread-safety for concurrency control.

2. **DagPipelineBuilder**:  
   This is a fluent API to simplify the creation of DAGs. It allows developers to easily create chained functions and inputs.

3. **Cell<T>**:  
   Represents a node in the DAG with reactive properties:
   - **Input Cells**: Contain a value that can be updated dynamically.
   - **Function Cells**: Compute their values based on input cell dependencies.
   - Provides subscription support for value change notifications.

4. **DagNode**:  
   Encapsulates the logic for individual nodes in the graph. Each node:
   - Tracks its dependencies.
   - Computes its value lazily or on demand.
   - Reacts to changes in its inputs.

## Potential use cases

- Manage complex backend tasks such as dynamic database connection pooling, orchestrating API requests, handling service dependencies, managing event-driven workflows, and enabling reactive monitoring and configuration updates in a modular and efficient way.
- Manage complex workflows involving real-time data processing, decision-making, and task dependencies, such as for transaction handling, risk analysis, or dynamic calculations.

## How it Works:
1. Define input cells to represent individual spreadsheet cells.
2. Use function cells to define formulas that depend on input cells.
3. Update the value of an input cell using `UpdateInput`.
4. The system recalculates and propagates changes to all dependent cells efficiently.

## Example 1:
An example of how to use the fluent api to build a simple DAG
<pre><code>
   var builder = DagPipelineBuilder.Create();
   builder.AddInput(6, out var cell1);
   builder.AddInput(4, out var cell2);
   builder.AddFunction(
      async inputs => (int)inputs[0] + (int)inputs[1],
      out Cell<int> functionCell,
      cell1, cell2
   );
   builder.Build();
   var result = await builder.GetResult<int>(functionCell);
   Console.WriteLine(result); 
</code></pre>

## Example 2:
Model and compute a set of dependent operations (inputs, functions, and their results) in a structured way.
The code below highlights how the the dag can run simulations where the inputs are updated dynamically, (in this case every 100 iterations) which automatically propagate through the DAG.
<pre><code>
static async Task Main()
{
    var dagPipelineBuilder = DagPipelineBuilder.Create();

    dagPipelineBuilder.AddInput<double>(GenerateRandomAssetPrice(), out var assetPrice)
           .AddInput<double>(GenerateRandomInterestRate(), out var interestRate);

    dagPipelineBuilder.AddFunction<double, double>(async inputs =>
    {
        var price = inputs[0];
        var rate = inputs[1];
        var futurePrice = price * Math.Exp(rate);
        return await Task.FromResult(futurePrice);
    }, out var simulationResult);

    var results = new List<double>();
    for (int i = 0; i < 1000; i++)
    {
        var dagEngine = dagPipelineBuilder.Build();

        if (i % 100 == 0)
        {
            dagPipelineBuilder.UpdateInput(assetPrice, GenerateRandomAssetPrice());
            dagPipelineBuilder.UpdateInput(interestRate, GenerateRandomInterestRate());
        }

        var result = await dagEngine.GetResult<double>(simulationResult);
        results.Add(result);
    }

    var averagePrice = results.Average();
    Console.WriteLine($"Average simulated future price: {averagePrice}");
}

private static double GenerateRandomAssetPrice()
{
    var random = new Random();
    return random.NextDouble() * 100 + 50;
}

private static double GenerateRandomInterestRate()
{
    var random = new Random();
    return random.NextDouble() * 0.1; 
}
</code></pre>

## Example 3:
Create a simple DAG that sums 3 inputs. When a cell is updated the results are recomputed dynamically.
<pre><code>
  var dagPipelineBuilder = DagPipelineBuilder.Create()
      .AddInput<double>(6.2, out var cell1)
      .AddInput<double>(4, out var cell2)
      .AddInput<double>(2, out var cell3)
      .AddFunction<double, double>(async inputs =>
      {
          var sum = inputs.Sum();
          return await Task.FromResult(sum);
      }, out var result, cell1, cell2, cell3);

  var dagEngine = dagPipelineBuilder.Build();

  Console.WriteLine($"Created cell1: {cell1.Value}, cell2: {cell2.Value}, and cell3: {cell3.Value}");
  Console.WriteLine($"Sum of cells: {await dagEngine.GetResult<double>(result)}");
  dagPipelineBuilder.UpdateInput(cell2, 5);
  dagPipelineBuilder.UpdateInput(cell3, 6);
  Console.WriteLine($"Updated Result: {await dagEngine.GetResult<double>(result)}");
</code></pre>

## Example 4:
This example shows a simple Markov Chain using ReactiveDag and how each computation depends on previous results, how derived values (like the most likely weather and expected sunny days) are automatically updated when inputs change, and how the dependency structure is managed.
<pre><code>
// Markov chain: Sunny, Cloudy, Rainy
double[,] transitionMatrix = { { 0.8, 0.15, 0.05 }, { 0.2, 0.6, 0.2 }, { 0.1, 0.3, 0.6 } };
double[] startState = { 1.0, 0.0, 0.0 };
string[] weatherLabels = { "Sunny", "Cloudy", "Rainy" };

var dag = DagPipelineBuilder.Create()
   .AddInput(transitionMatrix, out var matrixCell)
   .AddInput(startState, out var day0Cell);

// Next state function
double[] GetNextState(double[] current, double[,] matrix) =>
   Enumerable.Range(0, current.Length)
       .Select(j => Enumerable.Range(0, current.Length).Sum(i => current[i] * matrix[i, j]))
       .ToArray();

// Use explicit dependencies for all function nodes
var day0WithMatrix = dag.CombineCells(day0Cell, matrixCell);
dag.AddFunction(new[] { day0WithMatrix }, async inp => {
   var tuple = inp[0];
   return GetNextState((double[])tuple[0], (double[,])tuple[1]);
}, out var day1Cell);
var day1WithMatrix = dag.CombineCells(day1Cell, matrixCell);
dag.AddFunction(new[] { day1WithMatrix }, async inp => {
   var tuple = inp[0];
   return GetNextState((double[])tuple[0], (double[,])tuple[1]);
}, out var day2Cell);

// Most likely weather for each day
dag.AddFunction(new[] { day1Cell  }, async inp => weatherLabels[Array.IndexOf(inp[0], inp[0].Max())], out var day1Weather)
      .AddFunction(new[] { day2Cell }, async inp => weatherLabels[Array.IndexOf(inp[0], inp[0].Max())], out var day2Weather);

// Probability of rain for each day
dag.AddFunction(new[] { day1Cell as Cell<double[]> }, async inp => inp[0][2], out var rainProbDay1)
      .AddFunction(new[] { day2Cell as Cell<double[]> }, async inp => inp[0][2], out var rainProbDay2)
.Build();

async Task PrintResults()
{
   var probsDay1 = await dag.GetResult<double[]>(day1Cell);
   var probsDay2 = await dag.GetResult<double[]>(day2Cell);
   Console.WriteLine($"Day 1: {string.Join(", ", probsDay1.Select((p, i) => $"{weatherLabels[i]}: {p:P2}"))}, Most likely: {await dag.GetResult<string>(day1Weather)}, Rain: {await dag.GetResult<double>(rainProbDay1):P2}");
   Console.WriteLine($"Day 2: {string.Join(", ", probsDay2.Select((p, i) => $"{weatherLabels[i]}: {p:P2}"))}, Most likely: {await dag.GetResult<string>(day2Weather)}, Rain: {await dag.GetResult<double>(rainProbDay2):P2}");
}

Console.WriteLine("--- Initial ---");
await PrintResults();

Console.WriteLine("\n--- Now set initial state to 100% Rainy ---");
dag.UpdateInput(day0Cell, new double[] { 0, 0, 1 });
await PrintResults();
</code></pre>

## Nuget
ReactiveDag is available as a <a href="https://www.nuget.org/packages/ReactiveDAG">Nuget package.</a>
