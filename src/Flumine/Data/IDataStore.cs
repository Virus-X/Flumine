using System.Collections.Generic;

using Flumine.Util;

namespace Flumine.Data
{
    public interface IDataStore
    {
        /// <summary>
        /// Gets current master node.
        /// </summary>
        /// <returns>Current master node descriptor or null if no master currently.</returns>
        INodeDescriptor GetMaster();

        /// <summary>
        /// Tries to take master node to itself.
        /// </summary>
        /// <param name="node">Descriptor of current node.</param>
        /// <param name="deadNodeTimeout">Minimum timeout of <see cref="INodeDescriptor.LastSeen"/> property to assume that current master is dead. </param>
        /// <remarks>
        /// It's very important to perform the check whether master is to use <see cref="ServerClock"/>, because out of sync clock can lead to unpredictable system states.
        /// </remarks>
        /// <returns>True if master role successfully taken by node. False if another node already owns master role.</returns>
        bool TryTakeMasterRole(INodeDescriptor node, int deadNodeTimeout);

        /// <summary>
        /// Leaves master role and allows it to be taken by another node.
        /// </summary>
        /// <param name="node">Descriptor of current node.</param>
        void LeaveMasterRole(INodeDescriptor node);

        /// <summary>
        /// Updates <see cref="INodeDescriptor.LastSeen"/> with current timestamp.
        /// </summary>
        /// <param name="node">Descriptor of current node.</param>
        void RefreshLastSeen(INodeDescriptor node);

        /// <summary>
        /// Gets all nodes currently in cluster.
        /// </summary>
        /// <returns>List of all nodes in cluster.</returns>
        List<INodeDescriptor> GetAllNodes();

        /// <summary>
        /// Adds node to cluster node list.
        /// </summary>
        /// <param name="node">Descriptor of current node.</param>
        void Add(INodeDescriptor node);

        /// <summary>
        /// Removes node from cluster node list.
        /// </summary>
        /// <param name="node">Descriptor of current node.</param>
        void Remove(INodeDescriptor node);
    }
}

