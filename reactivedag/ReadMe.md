Reactive Directed Acyclic Graph (ReactiveDAG) Project
====================================================

Overview
--------
The ReactiveDAG project provides a flexible and scalable framework for managing reactive dependencies using a Directed Acyclic Graph (DAG). It allows developers to define a set of inputs and functions, track dependencies between them, and efficiently propagate changes throughout the system.

Key Components
--------------
### DagEngine:
The core engine responsible for managing nodes, updating values, and propagating changes. It provides functionality for:
- Adding inputs and functions as nodes.
- Handling cyclic dependency detection.
- Propagating updates and refreshing dependencies.
- Ensuring thread-safety for concurrency control.

### Builder:
A fluent API to simplify the creation of DAGs. It allows developers to:
- Add input cells with initial values.
- Define function cells that compute values based on dependencies.
- Update inputs and trigger recalculations.
- Build the final DagEngine for execution.

### Cell<T>:
Represents a node in the DAG with reactive properties:
- **Input Cells**: Contain a value that can be updated dynamically.
- **Function Cells**: Compute their values based on input cell dependencies.
- Provides subscription support for value change notifications.

### DagNode:
Encapsulates the logic for individual nodes in the graph. Each node:
- Tracks its dependencies.
- Computes its value lazily or on demand.
- Reacts to changes in its inputs.

### Potential use cases
- Manage complex backend tasks such as dynamic database connection pooling, orchestrating API requests, handling service dependencies, managing event-driven workflows, and enabling reactive monitoring and configuration updates in a modular and efficient way.
- Manage complex workflows involving real-time data processing, decision-making, and task dependencies, such as for transaction handling, risk analysis, or dynamic calculations.

How it Works:
-------------
- Define input cells using `AddInput` to represent individual cells.
- Use function cells using `AddFunction` to define functions that depend on input cells.
- Update the value of an input cell using `UpdateInput`.
- The system recalculates and propagates changes to all dependent cells efficiently.

### Examples
Please refer to the github page for some code examples.

### License
Apache-2.0 license