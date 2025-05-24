using System;
using System.Linq;
using System.Threading.Tasks;
using ReactiveDAG.Core.Engine;
using ReactiveDAG.Core.Models;

class Program
{
    static async Task Main()
    {
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

        // Day 1 and 2 states
        builder.AddFunction(async inp => Next((double[])inp[0], (double[,])inp[1]), out var state1, state0, matrixCell)
               .AddFunction(async inp => Next((double[])inp[0], (double[,])inp[1]), out var state2, state1, matrixCell);

        // Most likely weather for each day
        builder.AddFunction(async inp => weather[Array.IndexOf(((double[])inp[0]), ((double[])inp[0]).Max())], out var day1Weather, state1)
               .AddFunction(async inp => weather[Array.IndexOf(((double[])inp[0]), ((double[])inp[0]).Max())], out var day2Weather, state2);

        // Probability of rain for each day
        builder.AddFunction(async inp => ((double[])inp[0])[2], out var rain1, state1)
               .AddFunction(async inp => ((double[])inp[0])[2], out var rain2, state2)
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
    }
}