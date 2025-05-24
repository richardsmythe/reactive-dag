
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
      // 1. Define Markov Chain Parameters
     double[,] transitionMatrix = new double[,]
     {
         { 0.8, 0.15, 0.05 }, // Sunny -> [Sunny, Cloudy, Rainy]
         { 0.2, 0.6, 0.2 },   // Cloudy -> [Sunny, Cloudy, Rainy]
         { 0.1, 0.3, 0.6 }    // Rainy -> [Sunny, Cloudy, Rainy]
     };
     double[] initialState = new double[] { 1.0, 0.0, 0.0 }; // Start: 100% Sunny
     int days = 5;
     string[] weatherTypes = { "Sunny", "Cloudy", "Rainy" };

     // 2. Create the DAG and input cells
     var dag = new DagEngine();
     var matrixCell = dag.AddInput(transitionMatrix);
     var stateCells = new List<Cell<double[]>>();
     var firstStateCell = dag.AddInput(initialState);
     stateCells.Add(firstStateCell);

     // Compute next state from current state and transition matrix
     double[] NextState(double[] state, double[,] matrix)
     {
         int n = state.Length;
         double[] next = new double[n];
         for (int j = 0; j < n; j++)
             for (int i = 0; i < n; i++)
                 next[j] += state[i] * matrix[i, j];
         return next;
     }

     // 3. Add function cells for each day's state
     for (int day = 0; day < days; day++)
     {
         var prevStateCell = stateCells.Last();
         var nextStateCell = dag.AddFunction(
             new BaseCell[] { prevStateCell, matrixCell },
             async inputs => NextState((double[])inputs[0], (double[,])inputs[1])
         );
         stateCells.Add(nextStateCell);
     }

     // 4. Add derived cells for weather queries
     // Most probable weather label for each day
     var mostProbableWeatherCells = new List<Cell<string>>();
     for (int i = 1; i < stateCells.Count; i++)
     {
         var stateCell = stateCells[i];
         var mostProbableCell = dag.AddFunction(
             new BaseCell[] { stateCell },
             async inputs =>
             {
                 var probs = (double[])inputs[0];
                 int maxIdx = Array.IndexOf(probs, probs.Max());
                 return weatherTypes[maxIdx];
             }
         );
         mostProbableWeatherCells.Add(mostProbableCell);
     }

     // Probability of good weather (Sunny or Cloudy)
     var goodWeatherCells = new List<Cell<double>>();
     for (int i = 1; i < stateCells.Count; i++)
     {
         var stateCell = stateCells[i];
         var goodWeatherCell = dag.AddFunction(
             new BaseCell[] { stateCell },
             async inputs =>
             {
                 var probs = (double[])inputs[0];
                 return probs[0] + probs[1]; // Sunny + Cloudy
             }
         );
         goodWeatherCells.Add(goodWeatherCell);
     }

     // Probability of bad weather (Rainy)
     var badWeatherCells = new List<Cell<double>>();
     for (int i = 1; i < stateCells.Count; i++)
     {
         var stateCell = stateCells[i];
         var badWeatherCell = dag.AddFunction(
             new BaseCell[] { stateCell },
             async inputs =>
             {
                 var probs = (double[])inputs[0];
                 return probs[2]; // Rainy
             }
         );
         badWeatherCells.Add(badWeatherCell);
     }

     // Day with highest probability of rain
     var maxRainDayCell = dag.AddFunction(
         badWeatherCells.Cast<BaseCell>().ToArray(),
         async inputs =>
         {
             var rainProbs = inputs.Select(x => (double)x).ToArray();
             int maxIdx = Array.IndexOf(rainProbs, rainProbs.Max());
             return maxIdx + 1; // Day number (1-based)
         }
     );

     // Longest streak of likely sunny days
     var longestSunnyStreakCell = dag.AddFunction(
         mostProbableWeatherCells.Cast<BaseCell>().ToArray(),
         async inputs =>
         {
             int maxStreak = 0, current = 0;
             foreach (var w in inputs.Select(x => (string)x))
             {
                 if (w == "Sunny") current++;
                 else current = 0;
                 if (current > maxStreak) maxStreak = current;
             }
             return maxStreak;
         }
     );

     // Expected number of sunny days
     var expectedSunnyDaysCell = dag.AddFunction(
         stateCells.Skip(1).Cast<BaseCell>().ToArray(),
         async inputs => inputs.Select(arr => ((double[])arr)[0]).Sum()
     );

     // 5. Print initial state and transition matrix
     Console.WriteLine("Initial State: [" + string.Join(", ", initialState.Select(x => x.ToString("F2"))) + "]");
     Console.WriteLine("Transition Matrix:");
     for (int i = 0; i < transitionMatrix.GetLength(0); i++)
         Console.WriteLine("  [" + string.Join(", ", Enumerable.Range(0, transitionMatrix.GetLength(1)).Select(j => transitionMatrix[i, j].ToString("F2"))) + "]");
     Console.WriteLine();

     // 6. Print results for each day
     Console.WriteLine($"--- Weather probabilities and derived values for each day ---");
     for (int day = 1; day <= days; day++)
     {
         var probs = await dag.GetResult<double[]>(stateCells[day]);
         var mostLikely = await dag.GetResult<string>(mostProbableWeatherCells[day - 1]);
         var goodWeather = await dag.GetResult<double>(goodWeatherCells[day - 1]);
         var badWeather = await dag.GetResult<double>(badWeatherCells[day - 1]);
         Console.WriteLine($"\n--- Day {day} ---");
         Console.WriteLine($"Probabilities: {string.Join(", ", probs.Select((p, i) => $"{weatherTypes[i]}: {p:P2}"))}");
         Console.WriteLine($"Most likely weather: {mostLikely}");
         Console.WriteLine($"Good weather probability (Sunny or Cloudy): {goodWeather:P2}");
         Console.WriteLine($"Bad weather probability (Rainy): {badWeather:P2}");
     }

     // 7. Print aggregated results
     var expectedSunny = await dag.GetResult<double>(expectedSunnyDaysCell);
     var maxRainDay = await dag.GetResult<int>(maxRainDayCell);
     var longestSunnyStreak = await dag.GetResult<int>(longestSunnyStreakCell);
     Console.WriteLine($"\n--- Aggregated Results ---");
     Console.WriteLine($"Expected number of sunny days over {days} days: {expectedSunny:F2}");
     Console.WriteLine($"Day with highest probability of rain: Day {maxRainDay}");
     Console.WriteLine($"Longest streak of likely sunny days: {longestSunnyStreak}");

     // 8. Demonstrate dependency update: change initial state to 100% Rainy
     Console.WriteLine("\n--- Now changing initial state to 100% Rainy ---");
     await dag.UpdateInput(firstStateCell, new double[] { 0.0, 0.0, 1.0 });
     Console.WriteLine("Updated initial state: [0.00, 0.00, 1.00]");
     for (int day = 1; day <= days; day++)
     {
         var probs = await dag.GetResult<double[]>(stateCells[day]);
         var mostLikely = await dag.GetResult<string>(mostProbableWeatherCells[day - 1]);
         var goodWeather = await dag.GetResult<double>(goodWeatherCells[day - 1]);
         var badWeather = await dag.GetResult<double>(badWeatherCells[day - 1]);
         Console.WriteLine($"\n--- Day {day} ---");
         Console.WriteLine($"Probabilities: {string.Join(", ", probs.Select((p, i) => $"{weatherTypes[i]}: {p:P2}"))}");
         Console.WriteLine($"Most likely weather: {mostLikely}");
         Console.WriteLine($"Good weather probability (Sunny or Cloudy): {goodWeather:P2}");
         Console.WriteLine($"Bad weather probability (Rainy): {badWeather:P2}");
     }
     expectedSunny = await dag.GetResult<double>(expectedSunnyDaysCell);
     maxRainDay = await dag.GetResult<int>(maxRainDayCell);
     longestSunnyStreak = await dag.GetResult<int>(longestSunnyStreakCell);
     Console.WriteLine($"\n--- Aggregated Results (after initial state update) ---");
     Console.WriteLine($"Expected number of sunny days over {days} days: {expectedSunny:F2}");
     Console.WriteLine($"Day with highest probability of rain: Day {maxRainDay}");
     Console.WriteLine($"Longest streak of likely sunny days: {longestSunnyStreak}");

     // 9. Demonstrate dependency update: change transition matrix (make rain more likely)
     Console.WriteLine("\n--- Now making rain much more likely in the transition matrix ---");
     double[,] rainyMatrix = new double[,]
     {
         { 0.5, 0.2, 0.3 },
         { 0.1, 0.4, 0.5 },
         { 0.05, 0.25, 0.7 }
     };
     await dag.UpdateInput(matrixCell, rainyMatrix);
     Console.WriteLine("Updated transition matrix:");
     for (int i = 0; i < rainyMatrix.GetLength(0); i++)
         Console.WriteLine("  [" + string.Join(", ", Enumerable.Range(0, rainyMatrix.GetLength(1)).Select(j => rainyMatrix[i, j].ToString("F2"))) + "]");
     for (int day = 1; day <= days; day++)
     {
         var probs = await dag.GetResult<double[]>(stateCells[day]);
         var mostLikely = await dag.GetResult<string>(mostProbableWeatherCells[day - 1]);
         var goodWeather = await dag.GetResult<double>(goodWeatherCells[day - 1]);
         var badWeather = await dag.GetResult<double>(badWeatherCells[day - 1]);
         Console.WriteLine($"\n--- Day {day} ---");
         Console.WriteLine($"Probabilities: {string.Join(", ", probs.Select((p, i) => $"{weatherTypes[i]}: {p:P2}"))}");
         Console.WriteLine($"Most likely weather: {mostLikely}");
         Console.WriteLine($"Good weather probability (Sunny or Cloudy): {goodWeather:P2}");
         Console.WriteLine($"Bad weather probability (Rainy): {badWeather:P2}");
     }
     expectedSunny = await dag.GetResult<double>(expectedSunnyDaysCell);
     maxRainDay = await dag.GetResult<int>(maxRainDayCell);
     longestSunnyStreak = await dag.GetResult<int>(longestSunnyStreakCell);
     Console.WriteLine($"\n--- Aggregated Results (after matrix update) ----");
     Console.WriteLine($"Expected number of sunny days over {days} days: {expectedSunny:F2}");
     Console.WriteLine($"Day with highest probability of rain: Day {maxRainDay}");
     Console.WriteLine($"Longest streak of likely sunny days: {longestSunnyStreak}");

     // 10. Print DAG structure
     Console.WriteLine("\nDAG structure (cell dependencies):");
     for (int i = 1; i < stateCells.Count; i++)
         Console.WriteLine($"StateCell[{i}] depends on StateCell[{i - 1}] and matrixCell");
     Console.WriteLine("Each mostProbableWeatherCell depends on its corresponding StateCell.");
     Console.WriteLine("Each goodWeatherCell and badWeatherCell depends on its corresponding StateCell.");
     Console.WriteLine("maxRainDayCell merges all badWeatherCells.");
     Console.WriteLine("longestSunnyStreakCell merges all mostProbableWeatherCells.");
     Console.WriteLine("expectedSunnyDaysCell depends on all StateCells (except day 0).");
    }
</code></pre>

## Nuget
ReactiveDag is available as a <a href="https://www.nuget.org/packages/ReactiveDAG">Nuget package.</a>
