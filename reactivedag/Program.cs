using ReactiveDAG.Core.Engine;
using System;
using System.Linq;
using System.Threading.Tasks;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Builder.Create()
            .AddInput(6.2, out var cell1)
            .AddInput(4, out var cell2)
            .AddInput(2, out var cell3)
            .AddFunction(inputs =>
            {
                var sum = inputs.Select(i => Convert.ToDouble(i)).Sum();
                return sum;
            }, out var result)
            .Build();

        cell1.OnValueChanged = newValue => Console.WriteLine($"cell1 value changed to: {newValue}");
        cell2.OnValueChanged = newValue => Console.WriteLine($"cell2 value changed to: {newValue}");
        cell3.OnValueChanged = newValue => Console.WriteLine($"cell3 value changed to: {newValue}");

        Console.WriteLine($"Created cell1: {cell1.Value}, cell2: {cell2.Value}, and cell3: {cell3.Value}");
        Console.WriteLine($"Sum of cells: {await builder.GetResult<double>(result)}");

        builder.UpdateInput(cell2, 5);
        var updatedResult = await builder.GetResult<double>(result);
        Console.WriteLine($"Updated Result after changing cell2: {updatedResult}");
        
        builder.UpdateInput(cell3, 6); 
        
        updatedResult = await builder.GetResult<double>(result);
        Console.WriteLine($"Updated Result after changing cell3: {updatedResult}");

        Console.ReadLine();
    }
}
