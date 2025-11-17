using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReactiveDAG.Core.Models
{
    /// <summary>
    /// Interface for common operations on DAG nodes for strongly typed 
    /// way to access node properties regardless of generic type
    /// </summary>
    public interface IDagNodeOperations
    {
        /// <summary>
        /// Gets the node's dependencies
        /// </summary>
        HashSet<int> GetDependencies();

        /// <summary>
        /// Gets the node's cell
        /// </summary>
        BaseCell GetCell();

        /// <summary>
        /// Resets the computation for this node
        /// </summary>
        void ResetComputation();

        /// <summary>
        /// Evaluates the node and returns its value as an object
        /// </summary>
        Task<object> EvaluateAsync();
        
        /// <summary>
        /// Event that is triggered when the node's value is updated
        /// </summary>
        event Action NodeUpdated;

        /// <summary>
        /// Current status of the node computation
        /// </summary>
        NodeStatus Status { get; }

        /// <summary>
        /// Indicates if the node is currently computing
        /// </summary>
        bool IsComputing();

        /// <summary>
        /// Triggers the node updated notification in a non-generic way
        /// </summary>
        void NotifyUpdatedNode();

        /// <summary>
        /// Remove a specific dependency edge and dispose its subscription if present
        /// </summary>
        void RemoveDependency(int dependencyIndex);
    }
}