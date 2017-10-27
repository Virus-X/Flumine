using System;
using System.Collections.Generic;
using System.Linq;

using Flumine.Api;
using Flumine.Data;
using Flumine.Util;

namespace Flumine.Model
{
    /// <summary>
    /// Represents worker node
    /// </summary>
    public class Node : NodeDescriptor
    {
        private static readonly ILog Log = Logger.GetLoggerForDeclaringType();

        private readonly INodeApi api;

        public bool StateSynchronized { get; private set; }

        public int SharesCount
        {
            get
            {
                return AssignedShares.Count;
            }
        }

        public Node(INodeDescriptor descriptor, INodeApi api, FlumineHostConfig config)
            : base(descriptor, config)
        {
            this.api = api;
            StateSynchronized = false;
        }

        public void RefreshState()
        {
            try
            {
                var state = api.GetState();
                if (state.NodeId != NodeId)
                {
                    Log.WarnFormat("Expected to get node id {0} but got {1}. Marking data as obsolete", NodeId, state.NodeId);
                    // Different node id reported in state. That means that node record is obsolete.
                    MarkDead();
                    return;
                }

                StateSynchronized = true;
                MarkAlive();
                AssignedShares = new List<int>(state.AssignedShares ?? Enumerable.Empty<int>());
            }
            catch (Exception ex)
            {
                Log.DebugFormat("Failed to contact node {0}: {1}", this, ex.Message);
            }
        }

        public void AssignShares(Guid masterNodeId, IEnumerable<int> shares)
        {
            var shareAssignment = new ShareAssignmentArgs(masterNodeId, shares);
            api.AssignShares(shareAssignment);
            AddShares(shareAssignment.Shares);
        }

        public List<int> ReleaseShares(Guid masterNodeId, int count)
        {
            // Try to keep oldest assignments at this node.
            List<int> res = AssignedShares.AsEnumerable().Reverse().Take(count).ToList();
            if (res.Count > 0)
            {
                api.ReleaseShares(new ShareAssignmentArgs(masterNodeId, res));
                AssignedShares.RemoveRange(AssignedShares.Count - res.Count, res.Count);
            }

            return res;
        }
    }
}
