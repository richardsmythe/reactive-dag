using System;
using System.Threading;
using System.Threading.Tasks;
using ReactiveDAG.Core.Engine;
using ReactiveDAG.Core.Models;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Builder.Create()
                             .AddInput(2, out var input)
                             .AddFunction(inputs => (int)inputs[0] * 2, out var result)
                             .Build();

        using var cts = new CancellationTokenSource();
        var resultStream = builder.StreamResults(result, cts.Token);

        var streamingTask = Task.Run(async () =>
        {
            await foreach (var r in resultStream)
            {
                Console.WriteLine($"Streamed Result: {r}");
            }
        }, cts.Token);

        for (int i = 1; i <= 5; i++)
        {
            await builder.UpdateInput(input, i);
            await Task.Delay(1); // allow delay to show intermediate results
        }

        await streamingTask;
        cts.Cancel();
    }
}