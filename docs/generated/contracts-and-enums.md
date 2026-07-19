---
title: Contracts and enums
description: Shared interfaces, DTOs, statuses, and public model contracts.
---

Generated from commit [`254db11b060f`](https://github.com/richardsmythe/reactive-dag/commit/254db11b060f106bbd359c0f630770d3a792e474).
Every declaration below links to its immutable source line.

## `DagNodeDto`

```csharp
public class DagNodeDto
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNodeDto.cs:3`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeDto.cs#L3)

## `DagNodeDto.Index`

```csharp
public int Index
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNodeDto.cs:5`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeDto.cs#L5)

## `DagNodeDto.Type`

```csharp
public string Type
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNodeDto.cs:6`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeDto.cs#L6)

## `DagNodeDto.Value`

```csharp
public object Value
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNodeDto.cs:7`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeDto.cs#L7)

## `DagNodeDto.Dependencies`

```csharp
public int[] Dependencies
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNodeDto.cs:8`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeDto.cs#L8)

## `IDagNodeOperations`

```csharp
public interface IDagNodeOperations
```

Interface for common operations on DAG nodes for strongly typed way to access node properties regardless of generic type

[View source at `reactivedag/Models/IDagNodeOperations.cs:11`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/IDagNodeOperations.cs#L11)

## `NodeStatus`

```csharp
public enum NodeStatus
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/NodeStatus.cs:3`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/NodeStatus.cs#L3)
