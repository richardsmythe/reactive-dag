using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ReactiveDAG.Core.Engine;
using ReactiveDAG.Core.Models;

//
// Example: Weather Markov Chain using ReactiveDAG
// Demonstrates how to model and query a simple Markov process with a reactive DAG.
//
class Program
{
    static async Task Main(string[] args)
    {
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
        var stateCells = new List<BaseCell>();
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
        var mostProbableWeatherCells = new List<BaseCell>();
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
        var goodWeatherCells = new List<BaseCell>();
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
        var badWeatherCells = new List<BaseCell>();
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
            badWeatherCells.ToArray(),
            async inputs =>
            {
                var rainProbs = inputs.Select(x => (double)x).ToArray();
                int maxIdx = Array.IndexOf(rainProbs, rainProbs.Max());
                return maxIdx + 1; // Day number (1-based)
            }
        );

        // Longest streak of likely sunny days
        var longestSunnyStreakCell = dag.AddFunction(
            mostProbableWeatherCells.ToArray(),
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
            stateCells.Skip(1).ToArray(),
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
}