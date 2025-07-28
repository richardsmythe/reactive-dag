
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
   var DagPipelineBuilder = DagPipelineDagPipelineBuilder.Create();
   DagPipelineBuilder.AddInput(6, out var cell1)
        .AddInput(4, out var cell2)
        .AddFunction(
            async inputs => (int)inputs[0] + (int)inputs[1],
            out Cell<int> functionCell,
            cell1, cell2
        )
        .Build();   
   var result = await DagPipelineBuilder.GetResult<int>(functionCell);
   Assert.Equal(10, result);
</code></pre>

## Example 2:
Model and compute a set of dependent operations (inputs, functions, and their results) in a structured way.
The code below highlights how the the dag can run simulations where the inputs are updated dynamically, (in this case every 100 iterations) which automatically propagate through the DAG.
<pre><code>
  var DagPipelineBuilder = DagPipelineBuilder.Create();

  DagPipelineBuilder.AddInput(GenerateRandomAssetPrice(), out var assetPrice)
         .AddInput(GenerateRandomInterestRate(), out var interestRate);

  DagPipelineBuilder.AddFunction(inputs =>
  {
      var price = Convert.ToDouble(inputs[0]);
      var rate = Convert.ToDouble(inputs[1]);
      var futurePrice = price * Math.Exp(rate);
      return futurePrice;
  }, out var simulationResult);

  var results = new List<double>();
  for (int i = 0; i < 1000; i++)
  {
      var dagEngine = DagPipelineBuilder.Build();

      if (i % 100 == 0)
      {
          DagPipelineBuilder.UpdateInput(assetPrice, GenerateRandomAssetPrice());
          DagPipelineBuilder.UpdateInput(interestRate, GenerateRandomInterestRate());
      }

      var result = await dagEngine.GetResult<double>(simulationResult);
      results.Add(result);
  }

  var averagePrice = results.Average();
  Console.WriteLine($"Average simulated future price: {averagePrice}");
</code></pre>

## Example 3:
Create a simple DAG that sums 3 inputs. When a cell is updated the results are recomputed dynamically.
<pre><code>
var DagPipelineBuilder = DagPipelineBuilder.Create()
    .AddInput(6.2, out var cell1)
    .AddInput(4, out var cell2)
    .AddInput(2, out var cell3)
    .AddFunction(inputs =>
    {
        var sum = inputs.Select(i => Convert.ToDouble(i)).Sum();
        return sum;
    }, out var result)
    .Build();

Console.WriteLine($"Created cell1: {cell1.Value}, cell2: {cell2.Value}, and cell3: {cell3.Value}");
Console.WriteLine($"Sum of cells: {await DagPipelineBuilder.GetResult<double>(result)}");
await DagPipelineBuilder.UpdateInput(cell2, 5);
await DagPipelineBuilder.UpdateInput(cell3, 6);
Console.WriteLine($"Updated Result: {await DagPipelineBuilder.GetResult<double>(result)}");
</code></pre>

## Example 4:
Use of StreamResults() demonstrating real-time streaming of computed values while handling updates, cancellation, and graceful shutdown in an asynchronous workflow.
<pre><code>
var DagPipelineBuilder = DagPipelineBuilder.Create()
                    .AddInput(1, out var inputCell)
                    .AddFunction(inputs => (int)inputs[0] * 2, out var result)
                    .Build();

using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(2));

var resultStream = DagPipelineBuilder.StreamResults(result, cts.Token);

var streamingTask = Task.Run(async () =>
{
   try
   {
       await foreach (var r in resultStream.WithCancellation(cts.Token))
       {
           Console.WriteLine($"Streamed Result: {r}");
       }
   }
   catch (OperationCanceledException)
   {
       Console.WriteLine("Stream cancelled.");
   }
}, cts.Token);

// Simulate some task that changes inputCell's value
// The delay is added to see individual results
for (int i = 0; i <= 100; i++)
{
   await DagPipelineBuilder.UpdateInput(inputCell, i);
   await Task.Delay(5);
}            
await streamingTask;
</code></pre>

## Example 4:
This example shows a simple Markov Chain using ReactiveDag and how each computation depends on previous results, how derived values (like the most likely weather and expected sunny days) are automatically updated when inputs change, and how the dependency structure is managed.
<pre><code>
// Markov chain: Sunny, Cloudy, Rainy
double[,] matrix = { { 0.8, 0.15, 0.05 }, { 0.2, 0.6, 0.2 }, { 0.1, 0.3, 0.6 } };
double[] initial = { 1.0, 0.0, 0.0 };
string[] weather = { "Sunny", "Cloudy", "Rainy" };

var builder = DagPipelineBuilder.Create()
    .AddInput(matrix, out var matrixCell)
    .AddInput(initial, out var state0);

// Next state function
double[] Next(double[] s, double[,] m) =>
    Enumerable.Range(0, s.Length)
        .Select(j => Enumerable.Range(0, s.Length).Sum(i => s[i] * m[i, j]))
        .ToArray();

// Helper to combine two cells into a tuple cell (explicit dependencies)
Cell<(T1, T2)> CombineCells<T1, T2>(Cell<T1> c1, Cell<T2> c2)
{
    builder.AddFunction<(T1, T2)>(
        new BaseCell[] { c1, c2 },
        async inp => ((T1)inp[0], (T2)inp[1]),
        out var tupleCell
    );
    return tupleCell;
}

// Use explicit dependencies for all function nodes
var state0Tuple = CombineCells(state0, matrixCell);
Console.WriteLine("DAG after state0Tuple:");
Console.WriteLine(builder.ToJson());
builder.AddFunction<(double[], double[,]), double[]>(new[] { state0Tuple }, async inp => Next(inp[0].Item1, inp[0].Item2), out var state1);
Console.WriteLine("DAG after state1:");
Console.WriteLine(builder.ToJson());
var state1Tuple = CombineCells(state1, matrixCell);
Console.WriteLine("DAG after state1Tuple:");
Console.WriteLine(builder.ToJson());
builder.AddFunction<(double[], double[,]), double[]>(new[] { state1Tuple }, async inp => Next(inp[0].Item1, inp[0].Item2), out var state2);
Console.WriteLine("DAG after state2:");
Console.WriteLine(builder.ToJson());

// Most likely weather for each day
builder.AddFunction<double[], string>(new[] { state1 }, async inp => weather[Array.IndexOf(inp[0], inp[0].Max())], out var day1Weather)
       .AddFunction<double[], string>(new[] { state2 }, async inp => weather[Array.IndexOf(inp[0], inp[0].Max())], out var day2Weather);

// Probability of rain for each day
builder.AddFunction<double[], double>(new[] { state1 }, async inp => inp[0][2], out var rain1)
       .AddFunction<double[], double>(new[] { state2 }, async inp => inp[0][2], out var rain2)
.Build();

async Task Print()
{
    var s1 = await builder.GetResult<double[]>(state1);
    var s2 = await builder.GetResult<double[]>(state2);
    Console.WriteLine($"Day 1: {string.Join(", ", s1.Select((p, i) => $"{weather[i]}: {p:P2}"))}, Most likely: {await builder.GetResult<string>(day1Weather)}, Rain: {await builder.GetResult<double>(rain1):P2}");
    Console.WriteLine($"Day 2: {string.Join(", ", s2.Select((p, i) => $"{weather[i]}: {p:P2}"))}, Most likely: {await builder.GetResult<string>(day2Weather)}, Rain: {await builder.GetResult<double>(rain2):P2}");
}

Console.WriteLine("--- Initial ---");
await Print();

Console.WriteLine("\n--- Now set initial state to 100% Rainy ---");
builder.UpdateInput(state0, new double[] { 0, 0, 1 });
await Print();
</code></pre>

## Nuget
ReactiveDag is available as a <a href="https://www.nuget.org/packages/ReactiveDAG">Nuget package.</a>
