---
title: DagEngine
description: Create, update, inspect, stream, and remove nodes in a reactive directed acyclic graph.
---

Generated from commit [`254db11b060f`](https://github.com/richardsmythe/reactive-dag/commit/254db11b060f106bbd359c0f630770d3a792e474).
Every declaration below links to its immutable source line.

## `DagEngine`

```csharp
public partial class DagEngine : IDisposable
```

The main engine that manages the execution of the Directed Acyclic Graph (DAG). Handles the creation, updates, and removal of nodes and manages the dependencies between cells.

[View source at `reactivedag/Engine/DagEngine.cs:14`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L14)

## `DagEngine.GetNode`

```csharp
public DagNode<T> GetNode<T>(Cell<T> cell)
```

Returns the node

[View source at `reactivedag/Engine/DagEngine.cs:43`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L43)

## `DagEngine.GetAllNodes`

```csharp
public IEnumerable<IDagNodeOperations> GetAllNodes()
```

Returns all nodes in the DAG.

[View source at `reactivedag/Engine/DagEngine.cs:68`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L68)

## `DagEngine.NodeCount`

```csharp
public int NodeCount
```

Gets the total number of nodes in the DAG.

[View source at `reactivedag/Engine/DagEngine.cs:77`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L77)

## `DagEngine.ResultCallback`

```csharp
public delegate void ResultCallback<TResult>(TResult result)
```

Delegate for handling result callbacks.

[View source at `reactivedag/Engine/DagEngine.cs:89`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L89)

## `DagEngine.GetResult`

```csharp
public async Task<T> GetResult<T>(Cell<T> cell)
```

Retrieves the result for a specific cell asynchronously.

[View source at `reactivedag/Engine/DagEngine.cs:98`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L98)

## `DagEngine.StreamResults`

```csharp
public async IAsyncEnumerable<T> StreamResults<T>(Cell<T> cell, [EnumeratorCancellation] CancellationToken cancellationToken = default)
```

Streams the result of a specific cell asynchronously. The result is yielded whenever it changes.

[View source at `reactivedag/Engine/DagEngine.cs:152`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L152)

## `DagEngine.RemoveNode`

```csharp
public void RemoveNode<T>(Cell<T> cell)
```

Removes a node from the DAG and cleans up its dependencies.

[View source at `reactivedag/Engine/DagEngine.cs:205`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L205)

## `DagEngine.AddInput`

```csharp
public Cell<T> AddInput<T>(T value)
```

Adds a new input cell to the DAG.

[View source at `reactivedag/Engine/DagEngine.cs:247`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L247)

## `DagEngine.AddFunction (overload 1)`

```csharp
public Cell<TResult> AddFunction<TInputs, TResult>(Cell<TInputs>[] inputCells, Func<TInputs[], Task<TResult>> asyncFunction)
```

Adds a function node to the DAG, which is a computation dependent on other cells.

[View source at `reactivedag/Engine/DagEngine.cs:264`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L264)

## `DagEngine.AddFunction (overload 2)`

```csharp
public Cell<TResult> AddFunction<TResult>(BaseCell[] dependencies, Func<object[], Task<TResult>> function)
```

Adds a function node to the DAG that computes a result based on mixed-type input cells (BaseCell[]).

[View source at `reactivedag/Engine/DagEngine.cs:329`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L329)

## `DagEngine.IsCyclic`

```csharp
public bool IsCyclic(int startIndex, int targetIndex)
```

Checks if there is a cyclic dependency between two nodes in the DAG.

[View source at `reactivedag/Engine/DagEngine.cs:394`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L394)

## `DagEngine.HasChanged`

```csharp
public bool HasChanged<T>(Cell<T> cell)
```

Checks if the value of a given cell has changed.

[View source at `reactivedag/Engine/DagEngine.cs:422`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L422)

## `DagEngine.UpdateInput`

```csharp
public async Task UpdateInput<T>(Cell<T> cell, T value)
```

Updates the value of an input cell in the DAG and triggers updates for dependent nodes.

[View source at `reactivedag/Engine/DagEngine.cs:434`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L434)

## `DagEngine.Dispose`

```csharp
public void Dispose()
```

Releases resources held by the DagEngine.

[View source at `reactivedag/Engine/DagEngine.cs:520`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/DagEngine.cs#L520)
