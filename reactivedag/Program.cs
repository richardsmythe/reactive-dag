using System;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveDAG.Core.Engine;
using ReactiveDAG.Core.Models;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Builder.Create()
            .AddInput(1, out var inputCell)
            .AddFunction(async inputs => (int)inputs[0] * 2, out var result)
            .Build();

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        var resultStream = builder.StreamResults(result, cts.Token);
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
            await builder.UpdateInput(inputCell, i);
            await Task.Delay(5);
        }

        await streamingTask;

    }
   
}
