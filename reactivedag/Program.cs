using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ReactiveDAG.Core.Engine;
using ReactiveDAG.Core.Models;

class Program
{
    static async Task Main(string[] args)
    {
        // Demo ReactiveDag for a simple weather Markov Chain: Sunny, Cloudy, Rainy
        // Transition matrix represents the probability of moving from one weather state to another
        double[,] transitionMatrix = new double[,]
        {
            // To:            Sunny   Cloudy  Rainy
            /* From Sunny */ { 0.8,    0.15,   0.05 },
            /* From Cloudy*/ { 0.2,    0.6,    0.2  },
            /* From Rainy */ { 0.1,    0.3,    0.6  }
        };

        // Start 100% chance of Sunny on day 0
        double[] initialState = new double[] { 1.0, 0.0, 0.0 };
        int days = 5;

        var dag = new DagEngine();
        var matrixCell = dag.AddInput(transitionMatrix);
        var stateCells = new List<BaseCell>();
        var firstStateCell = dag.AddInput(initialState);
        stateCells.Add(firstStateCell);

        // Compute the weather probabilities for the next day based on today's state and transition matrix
        Func<double[], double[,], double[]> stepFunc = (state, matrix) =>
        {
            int n = state.Length;
            double[] next = new double[n];
            for (int j = 0; j < n; j++)
            {
                next[j] = 0;
                for (int i = 0; i < n; i++)
                    next[j] += state[i] * matrix[i, j];
            }
            return next;
        };

        // Add function cells for each day's state
        for (int day = 0; day < days; day++)
        {
            var prevStateCell = stateCells.Last();
            var nextStateCell = dag.AddFunction(
                new BaseCell[] { prevStateCell, matrixCell },
                async inputs => stepFunc((double[])inputs[0], (double[,])inputs[1])
            );
            stateCells.Add(nextStateCell);
        }

        string[] weatherTypes = { "Sunny", "Cloudy", "Rainy" };

        // Add function cells to compute the most probable weather for each day
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

        // Add a function cell to compute the expected number of sunny days
        var expectedSunnyDaysCell = dag.AddFunction(
            stateCells.Skip(1).ToArray(),
            async inputs => inputs.Select(arr => ((double[])arr)[0]).Sum()
        );

        // Print results for each day
        Console.WriteLine($"Weather probabilities and most likely weather for each day:");
        for (int day = 1; day <= days; day++)
        {
            var probs = await dag.GetResult<double[]>(stateCells[day]);
            var mostLikely = await dag.GetResult<string>(mostProbableWeatherCells[day - 1]);
            Console.Write($"Day {day}: ");
            for (int i = 0; i < probs.Length; i++)
                Console.Write($"{weatherTypes[i]}: {probs[i]:P2}  ");
            Console.WriteLine($"| Most likely: {mostLikely}");
        }

        var expectedSunny = await dag.GetResult<double>(expectedSunnyDaysCell);
        Console.WriteLine($"\nExpected number of sunny days over {days} days: {expectedSunny:F2}");

        // Demonstrate dependency update: change initial state to 100% Rainy
        Console.WriteLine("\n--- Now changing initial state to 100% Rainy ---");
        await dag.UpdateInput(firstStateCell, new double[] { 0.0, 0.0, 1.0 });
        for (int day = 1; day <= days; day++)
        {
            var probs = await dag.GetResult<double[]>(stateCells[day]);
            var mostLikely = await dag.GetResult<string>(mostProbableWeatherCells[day - 1]);
            Console.Write($"Day {day}: ");
            for (int i = 0; i < probs.Length; i++)
                Console.Write($"{weatherTypes[i]}: {probs[i]:P2}  ");
            Console.WriteLine($"| Most likely: {mostLikely}");
        }
        expectedSunny = await dag.GetResult<double>(expectedSunnyDaysCell);
        Console.WriteLine($"\nExpected number of sunny days over {days} days: {expectedSunny:F2}");

        // Print a simple dependency list
        Console.WriteLine("\nDAG structure (cell dependencies):");
        for (int i = 1; i < stateCells.Count; i++)
        {
            Console.WriteLine($"StateCell[{i}] depends on StateCell[{i - 1}] and matrixCell");
        }
        Console.WriteLine("Each mostProbableWeatherCell depends on its corresponding StateCell.");
        Console.WriteLine("expectedSunnyDaysCell depends on all StateCells (except day 0).");
    }
}