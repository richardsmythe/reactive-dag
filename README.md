![NuGet Downloads](https://img.shields.io/nuget/dt/reactivedag)

# ReactiveDAG

A reactive DAG engine for .NET 8. You define inputs and computations, wire them together, and changes propagate through the graph automatically.

Good for simulations, multi-step calculations where intermediate results get reused, build/task orchestration, or anywhere you'd reach for a spreadsheet-like dependency model.

## Architecture

There are two ways to use this:

**`DagPipelineBuilder`** — fluent API for building pipelines. You chain `.AddInput().AddFunction()` and it tracks the wiring for you. Best for straightforward linear/fan-in pipelines.

**`DagEngine`** — the underlying engine. Use it directly when you need explicit dependency arrays, node removal, streaming, or want to inspect the graph at runtime.

Under the hood, **`Cell<T>`** holds a value and **`DagNode<T>`** wraps it with computation logic. When an input changes, the engine walks dependents and recomputes what's needed.

### Components

- **DagEngine** — manages nodes, propagates updates, detects cycles, handles concurrency
- **DagPipelineBuilder** — fluent builder on top of `DagEngine`
- **Cell\<T\>** — input cells hold values directly; function cells get their values from a compute function
- **DagNode\<T\>** — wraps a cell, tracks dependencies, handles lazy/on-demand computation

## API Reference

### Creating Inputs

```csharp
// Via builder
builder.AddInput(42, out Cell<int> cell);

// Via engine directly
Cell<int> cell = engine.AddInput(42);
```

### Adding Functions

```csharp
// All inputs share a type
builder.AddFunction<int, int>(async inputs => inputs.Sum(), out var sum);

// Mixed types — pass explicit BaseCell[] dependencies
engine.AddFunction<string>(
    new BaseCell[] { intCell, dateCell },
    async inputs => $"{inputs[0]} on {inputs[1]}");

// Explicit typed dependencies
engine.AddFunction<int, int>(new[] { a, b }, async inputs => inputs[0] + inputs[1]);
```

### Updating Inputs

```csharp
await engine.UpdateInput(cell, newValue);
```

This triggers recomputation of everything downstream.

### Getting Results

```csharp
var value = await engine.GetResult(cell);
```

### Streaming

Subscribe to a cell's value over time:

```csharp
await foreach (var value in engine.StreamResults(cell, cancellationToken))
{
    Console.WriteLine(value);
}
```

Yields the current value first, then emits whenever the cell recomputes.

### Inspecting the Graph

```csharp
int count = engine.NodeCount;

foreach (var node in engine.GetAllNodes())
{
    var cell = node.GetCell();
    var deps = node.GetDependencies();
    var value = await node.EvaluateAsync();
}

bool changed = engine.HasChanged(myCell);
```

### Removing Nodes

```csharp
engine.RemoveNode(cell);
```

### Combining Mixed-Type Cells

```csharp
Cell<object[]> combined = builder.CombineCells(intCell, stringCell, boolCell);
```

## Execution Model

- All compute functions are async (`Func<T[], Task<TResult>>`).
- `UpdateInput` walks dependents in topological order and recomputes them.
- Multiple inputs to a function node resolve concurrently (`Task.WhenAll`).
- If a node recomputes to the same value, propagation stops there (uses `EqualityComparer<T>.Default`).
- `AddFunction` checks for cycles immediately and rolls back if one would form.
- Thread-safe: atomic index generation, per-node compute locks, global semaphore for propagation.

## Use Cases

- Backend orchestration: API request pipelines, service dependencies, event-driven workflows
- Financial calculations: risk analysis, transaction chains, dynamic pricing
- Simulations with inputs that change over time

## How it Works

1. Create input cells with initial values.
2. Add function cells that depend on those inputs (or on other function cells).
3. Call `UpdateInput` when something changes.
4. The engine recomputes everything downstream automatically.

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

## Roadmap

**What's in place**
- Fluent builder API
- Async propagation with parallel dependency resolution
- Change deduplication
- Cycle detection with rollback
- Streaming via `IAsyncEnumerable`
- Thread safety (atomic indexing, per-node locks, propagation serialization)

**Ideas / contributions welcome**
- Error propagation to dependents (fault states)
- Batch/transactional updates
- Graph export (DOT/Mermaid)
- Observability hooks
- Benchmarks

Open an issue if there's something specific you'd like to see.
