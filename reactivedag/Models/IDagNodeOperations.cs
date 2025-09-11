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
    }
}