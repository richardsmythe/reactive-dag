Reactive Directed Acyclic Graph (ReactiveDAG) Project
====================================================

Overview
--------
The ReactiveDAG project provides a flexible and scalable framework for managing reactive dependencies using a Directed Acyclic Graph (DAG). It allows developers to define a set of inputs and functions, track dependencies between them, and efficiently propagate changes throughout the system.

* **Declarative Dependency Management:** Define dependencies between data and computations in a clear and concise way, making your code easier to understand and maintain.
* **Automatic Change Propagation:**  When some data changes, ReactiveDAG automatically updates all dependent computations, ensuring consistency and eliminating manual update logic.
* **Asynchronous Operations Support:** Seamlessly integrate asynchronous operations (e.g., database queries, network requests) into your reactive workflows.
* **Improved Code Structure:**  Separate data, computations, and dependencies, leading to a more organised and testable codebase.
* **Scalability:**  ReactiveDAG is designed to handle complex dependencies and large-scale data flows efficiently.

Key Components
--------------
## DagEngine
The core engine responsible for managing nodes, updating values, and propagating changes. It provides functionality for:
- Adding inputs and functions as nodes.
- Handling cyclic dependency detection.
- Propagating updates and refreshing dependencies.
- Ensuring thread-safety for concurrency control.

## Builder
A fluent API to simplify the creation of DAGs. It allows developers to:
- Add input cells with initial values.
- Define function cells that compute values based on dependencies.
- Update inputs and trigger recalculations.
- Build the final `DagEngine` for execution.

## BaseCell
An abstract class that represents a reactive node in the DAG. It:
- Supports `Subscribe` functionality for observing changes.
- Notifies all observers when the value changes.
- Manages `CellType` (Input or Function) and an `Index` for tracking.

## Cell<T>
Represents a reactive node with strongly-typed values. It includes:
- **Input Cells**: Stores a value that can be updated dynamically.
- **Function Cells**: Computes values based on input cell dependencies.
- Stores the `PreviousValue` to track changes.
- Notifies subscribers when the value changes.

## DagNode
Encapsulates the logic for individual nodes in the graph. Each node:
- Tracks its dependencies.
- Computes its value asynchronously and caches results.
- Reacts to changes in its inputs and notifies observers.
- Ensures thread safety using a semaphore lock.

## DagNodeBase
The base class for DAG nodes, providing:
- A reference to the associated `BaseCell`.
- A mechanism to compute node values asynchronously.
- Lazy evaluation and subscription management for dependency tracking.
- Automatic recomputation when dependencies change.

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
MIT