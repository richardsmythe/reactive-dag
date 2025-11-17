using ReactiveDAG.Core.Engine;
using ReactiveDAG.Core.Models;

class Program
{
    static async Task Main()
    {
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
    }
}