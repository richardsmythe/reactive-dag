---
title: DagPipelineBuilder
description: Build typed and mixed-type pipelines through ReactiveDAG's fluent API.
---

Generated from commit [`254db11b060f`](https://github.com/richardsmythe/reactive-dag/commit/254db11b060f106bbd359c0f630770d3a792e474).
Every declaration below links to its immutable source line.

## `DagPipelineBuilder`

```csharp
public class DagPipelineBuilder
```

A builder class for constructing a Directed Acyclic Graph (DAG) using the `DagEngine`.

[View source at `reactivedag/Engine/Builder.cs:8`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L8)

## `DagPipelineBuilder.DagPipelineBuilder`

```csharp
public DagPipelineBuilder()
```

Initializes a new instance of the `DagPipelineBuilder` class.

[View source at `reactivedag/Engine/Builder.cs:16`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L16)

## `DagPipelineBuilder.Create`

```csharp
public static DagPipelineBuilder Create()
```

Creates a new instance of the `DagPipelineBuilder` class.

[View source at `reactivedag/Engine/Builder.cs:25`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L25)

## `DagPipelineBuilder.AddInput (overload 1)`

```csharp
public DagPipelineBuilder AddInput<T>(T value, out Cell<T> cell)
```

Adds an input value to the DAG and returns the created cell.

[View source at `reactivedag/Engine/Builder.cs:37`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L37)

## `DagPipelineBuilder.AddInput (overload 2)`

```csharp
public DagPipelineBuilder AddInput<T>(T value)
```

Adds an input value to the DAG.

[View source at `reactivedag/Engine/Builder.cs:50`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L50)

## `DagPipelineBuilder.AddFunction (overload 1)`

```csharp
public DagPipelineBuilder AddFunction<TInputs, TResult>(Func<TInputs[], Task<TResult>> function, out Cell<TResult> resultCell)
```

Adds a function node to the DAG that computes a result based on its input cells.

[View source at `reactivedag/Engine/Builder.cs:65`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L65)

## `DagPipelineBuilder.AddFunction (overload 2)`

```csharp
public DagPipelineBuilder AddFunction<TInputs, TResult>(Func<TInputs[], Task<TResult>> function)
```

Adds a function node to the DAG that computes a result based on its input cells.

[View source at `reactivedag/Engine/Builder.cs:88`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L88)

## `DagPipelineBuilder.AddFunction (overload 3)`

```csharp
public DagPipelineBuilder AddFunction<TInputs, TResult>(Cell<TInputs>[] dependencies, Func<TInputs[], Task<TResult>> function, out Cell<TResult> resultCell)
```

Adds a function node to the DAG that computes a result based on an explicit set of dependency cells.

[View source at `reactivedag/Engine/Builder.cs:106`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L106)

## `DagPipelineBuilder.AddFunction (overload 4)`

```csharp
public DagPipelineBuilder AddFunction<TInputs, TResult>( Func<TInputs[], Task<TResult>> function, out Cell<TResult> resultCell, params Cell<TInputs>[] dependencies)
```

Adds a function node to the DAG that computes a result based on a variable number of dependency cells.

[View source at `reactivedag/Engine/Builder.cs:123`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L123)

## `DagPipelineBuilder.AddFunction (overload 5)`

```csharp
public DagPipelineBuilder AddFunction<TResult>(BaseCell[] dependencies, Func<object[], Task<TResult>> function, out Cell<TResult> resultCell)
```

Adds a function node to the DAG that computes a result based on an explicit set of dependency cells of any type.

[View source at `reactivedag/Engine/Builder.cs:142`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L142)

## `DagPipelineBuilder.UpdateInput`

```csharp
public DagPipelineBuilder UpdateInput<T>(Cell<T> cell, T newValue)
```

Updates an existing input cell in the DAG with a new value.

[View source at `reactivedag/Engine/Builder.cs:157`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L157)

## `DagPipelineBuilder.RemoveNode`

```csharp
public DagPipelineBuilder RemoveNode(BaseCell cell)
```

Removes a node from the DAG and updates the builder's cell list.

[View source at `reactivedag/Engine/Builder.cs:168`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L168)

## `DagPipelineBuilder.Build`

```csharp
public DagEngine Build()
```

Builds and returns the constructed `DagEngine`.

[View source at `reactivedag/Engine/Builder.cs:181`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L181)

## `DagPipelineBuilder.GetResult`

```csharp
public async Task<T> GetResult<T>(BaseCell cell)
```

Gets the result for a specific cell asynchronously using the underlying DagEngine.

[View source at `reactivedag/Engine/Builder.cs:192`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L192)

## `DagPipelineBuilder.CombineCells`

```csharp
public Cell<object[]> CombineCells(params BaseCell[] cells)
```

Combines any number of cells into a single function cell whose value is an object array containing the values of the input cells.

[View source at `reactivedag/Engine/Builder.cs:205`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Engine/Builder.cs#L205)
