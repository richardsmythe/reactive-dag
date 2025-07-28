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

        // Use explicit dependencies for all function nodes
        var state0Tuple = builder.CombineCells(state0, matrixCell);
        Console.WriteLine("DAG after state0Tuple:");
        Console.WriteLine(builder.ToJson());
        builder.AddFunction(new[] { state0Tuple }, async inp => {
            var tuple = inp[0];
            return Next((double[])tuple[0], (double[,])tuple[1]);
        }, out var state1);
        Console.WriteLine("DAG after state1:");
        Console.WriteLine(builder.ToJson());
        var state1Tuple = builder.CombineCells(state1, matrixCell);
        Console.WriteLine("DAG after state1Tuple:");
        Console.WriteLine(builder.ToJson());
        builder.AddFunction(new[] { state1Tuple }, async inp => {
            var tuple = inp[0];
            return Next((double[])tuple[0], (double[,])tuple[1]);
        }, out var state2);
        Console.WriteLine("DAG after state2:");
        Console.WriteLine(builder.ToJson());

        // Most likely weather for each day
        builder.AddFunction(new[] { state1  }, async inp => weather[Array.IndexOf(inp[0], inp[0].Max())], out var day1Weather)
               .AddFunction(new[] { state2 }, async inp => weather[Array.IndexOf(inp[0], inp[0].Max())], out var day2Weather);

        // Probability of rain for each day
        builder.AddFunction(new[] { state1 as Cell<double[]> }, async inp => inp[0][2], out var rain1)
               .AddFunction(new[] { state2 as Cell<double[]> }, async inp => inp[0][2], out var rain2)
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