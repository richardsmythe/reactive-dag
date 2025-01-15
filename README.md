
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

2. **Builder**:  
   A fluent API to simplify the creation of DAGs. It allows developers to:
   - Add input cells with initial values.
   - Define function cells that compute values based on dependencies.
   - Update inputs and trigger recalculations.
   - Build the final `DagEngine` for execution.

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
- 
## How it Works:
1. Define input cells to represent individual spreadsheet cells.
2. Use function cells to define formulas that depend on input cells.
3. Update the value of an input cell using `UpdateInput`.
4. The system recalculates and propagates changes to all dependent cells efficiently.

## Basic example:
Model and compute a set of dependent operations (inputs, functions, and their results) in a structured way.
The code below highlights how the inputs can be updated (in this case every 100 iterations) which automatically propagate through the DAG.
<pre><code>
        var builder = Builder.Create();
        builder.AddInput(GenerateRandomAssetPrice(), out var assetPrice)
               .AddInput(GenerateRandomInterestRate(), out var interestRate);
        builder.AddFunction(inputs =>
        {
            var price = Convert.ToDouble(inputs[0]);
            var rate = Convert.ToDouble(inputs[1]);
            var futurePrice = price * Math.Exp(rate);
            return futurePrice;
        }, out var simulationResult);
        var results = new List<double>();
        for (int i = 0; i < 1000; i++)
        {
            var dagEngine = builder.Build();
            if (i % 100 == 0)
            {
                builder.UpdateInput(assetPrice, GenerateRandomAssetPrice());
                builder.UpdateInput(interestRate, GenerateRandomInterestRate());
            }
            var result = await dagEngine.GetResult<double>(simulationResult);
            results.Add(result);
        }
        var averagePrice = results.Average();
        Console.WriteLine($"Average simulated future price: {averagePrice}");
</code></pre>


