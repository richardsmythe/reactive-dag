---
title: Cells and nodes
description: Values, computations, subscriptions, and lazy evaluation primitives.
---

Generated from commit [`254db11b060f`](https://github.com/richardsmythe/reactive-dag/commit/254db11b060f106bbd359c0f630770d3a792e474).
Every declaration below links to its immutable source line.

## `BaseCell`

```csharp
public abstract class BaseCell : IObservable<object>
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/BaseCell.cs:3`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/BaseCell.cs#L3)

## `BaseCell.Index`

```csharp
public int Index
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/BaseCell.cs:6`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/BaseCell.cs#L6)

## `BaseCell.CellType`

```csharp
public CellType CellType
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/BaseCell.cs:7`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/BaseCell.cs#L7)

## `BaseCell.Subscribe (overload 1)`

```csharp
public abstract IDisposable Subscribe(Func<object, Task> onChanged)
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/BaseCell.cs:9`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/BaseCell.cs#L9)

## `BaseCell.Subscribe (overload 2)`

```csharp
public IDisposable Subscribe(IObserver<object> observer)
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/BaseCell.cs:11`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/BaseCell.cs#L11)

## `Cell`

```csharp
public class Cell<T> : BaseCell
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/Cell.cs:7`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/Cell.cs#L7)

## `Cell.Value`

```csharp
public T Value
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/Cell.cs:10`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/Cell.cs#L10)

## `Cell.PreviousValue`

```csharp
public T PreviousValue
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/Cell.cs:23`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/Cell.cs#L23)

## `Cell.OnValueChanged`

```csharp
public Action<T> OnValueChanged
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/Cell.cs:24`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/Cell.cs#L24)

## `Cell.Cell`

```csharp
public Cell(int index, CellType type, T value)
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/Cell.cs:26`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/Cell.cs#L26)

## `Cell.CreateInputCell`

```csharp
public static Cell<T> CreateInputCell(int index, T value)
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/Cell.cs:34`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/Cell.cs#L34)

## `Cell.CreateFunctionCell`

```csharp
public static Cell<T> CreateFunctionCell(int index)
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/Cell.cs:35`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/Cell.cs#L35)

## `Cell.Subscribe (overload 1)`

```csharp
public IDisposable Subscribe(Action<T> onChanged)
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/Cell.cs:37`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/Cell.cs#L37)

## `Cell.Subscribe (overload 2)`

```csharp
public override IDisposable Subscribe(Func<object, Task> onChanged)
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/Cell.cs:43`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/Cell.cs#L43)

## `CellType`

```csharp
public enum CellType
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/Cell.cs:68`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/Cell.cs#L68)

## `DagNode`

```csharp
public class DagNode<T> : DagNodeBase<T>
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNode.cs:5`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNode.cs#L5)

## `DagNode.DagNode`

```csharp
public DagNode(Cell<T> cell, Func<Task<T>> computeValue) : base(cell, computeValue)
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNode.cs:11`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNode.cs#L11)

## `DagNode.GetCellValue`

```csharp
public T GetCellValue()
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNode.cs:17`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNode.cs#L17)

## `DagNode.NotifyUpdatedNode`

```csharp
public void NotifyUpdatedNode()
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNode.cs:26`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNode.cs#L26)

## `DagNode.ComputeNodeValueAsync`

```csharp
public override async Task<T> ComputeNodeValueAsync()
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNode.cs:32`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNode.cs#L32)

## `DagNode.ResetComputation`

```csharp
public override void ResetComputation()
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNode.cs:72`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNode.cs#L72)

## `DagNodeBase`

```csharp
public abstract class DagNodeBase<T> : IDagNodeOperations
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNodeBase.cs:8`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L8)

## `DagNodeBase.Cell`

```csharp
public BaseCell Cell
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNodeBase.cs:10`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L10)

## `DagNodeBase.Dependencies`

```csharp
public HashSet<int> Dependencies
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNodeBase.cs:11`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L11)

## `DagNodeBase.DeferredComputedNodeValue`

```csharp
public Lazy<Task<T>> DeferredComputedNodeValue
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNodeBase.cs:12`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L12)

## `DagNodeBase.Subscriptions`

```csharp
public List<IDisposable> Subscriptions
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNodeBase.cs:13`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L13)

## `DagNodeBase.Status`

```csharp
public NodeStatus Status
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNodeBase.cs:14`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L14)

## `DagNodeBase.NodeUpdated`

```csharp
public event Action NodeUpdated
```

This public member currently has no XML summary in the source snapshot.

[View source at `reactivedag/Models/DagNodeBase.cs:18`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L18)

## `DagNodeBase.ComputeNodeValueAsync`

```csharp
public abstract Task<T> ComputeNodeValueAsync()
```

Concrete nodes implement this to actually produce their value.

[View source at `reactivedag/Models/DagNodeBase.cs:49`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L49)

## `DagNodeBase.ConnectDependencies`

```csharp
public void ConnectDependencies(IEnumerable<BaseCell> dependencyCells, Func<Task<T>> computeNodeValue)
```

Subscribes to dependency cells and wires them into the computation pipeline.

[View source at `reactivedag/Models/DagNodeBase.cs:67`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L67)

## `DagNodeBase.DisposeSubscriptions`

```csharp
public void DisposeSubscriptions()
```

Disposes every dependency subscription tracked by this node.

[View source at `reactivedag/Models/DagNodeBase.cs:135`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L135)

## `DagNodeBase.GetDependencies`

```csharp
public HashSet<int> GetDependencies()
```

Returns the dependency ids tracked by this node.

[View source at `reactivedag/Models/DagNodeBase.cs:153`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L153)

## `DagNodeBase.GetCell`

```csharp
public BaseCell GetCell()
```

Returns the wrapped cell.

[View source at `reactivedag/Models/DagNodeBase.cs:158`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L158)

## `DagNodeBase.ResetComputation`

```csharp
public abstract void ResetComputation()
```

Allows derived nodes to reset their cached computation state.

[View source at `reactivedag/Models/DagNodeBase.cs:163`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L163)

## `DagNodeBase.EvaluateAsync`

```csharp
public virtual async Task<object> EvaluateAsync()
```

Evaluates the node and returns the value boxed as an object.

[View source at `reactivedag/Models/DagNodeBase.cs:168`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L168)

## `DagNodeBase.IsComputing`

```csharp
public bool IsComputing()
```

Indicates whether the node is currently computing.

[View source at `reactivedag/Models/DagNodeBase.cs:177`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L177)

## `DagNodeBase.NotifyUpdatedNode`

```csharp
public void NotifyUpdatedNode()
```

Raises the NodeUpdated event.

[View source at `reactivedag/Models/DagNodeBase.cs:182`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L182)

## `DagNodeBase.RemoveDependency`

```csharp
public void RemoveDependency(int dependencyIndex)
```

Removes a specific dependency and disposes its subscription.

[View source at `reactivedag/Models/DagNodeBase.cs:187`](https://github.com/richardsmythe/reactive-dag/blob/254db11b060f106bbd359c0f630770d3a792e474/reactivedag/Models/DagNodeBase.cs#L187)
