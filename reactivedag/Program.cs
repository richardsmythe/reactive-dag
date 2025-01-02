
using ReactiveDAG.Core.Engine;

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

        // Set up OnValueChanged callbacks for each cell
        cell1.OnValueChanged = newValue => Console.WriteLine($"cell1 value changed to: {newValue}");
        cell2.OnValueChanged = newValue => Console.WriteLine($"cell2 value changed to: {newValue}");
        cell3.OnValueChanged = newValue => Console.WriteLine($"cell3 value changed to: {newValue}");

        Console.WriteLine($"Created cell1: {cell1.Value}, cell2: {cell2.Value}, and cell3: {cell3.Value}");
        Console.WriteLine($"Sum of cells: {await builder.GetResult<double>(result)}");

        // Update cell2
        builder.UpdateInput(cell2, 5);
        Console.WriteLine($"cell2 updated to: {cell2.Value}");

        // Update cell3
        builder.UpdateInput(cell3, 6);
        Console.WriteLine($"cell3 updated to: {cell3.Value}");

        Console.WriteLine($"Updated Result: {await builder.GetResult<double>(result)}");

        Console.ReadLine();
    }
}
