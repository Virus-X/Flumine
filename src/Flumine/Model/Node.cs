using System;
using System.Collections.Generic;
using System.Linq;

using Flumine.Api;
using Flumine.Nancy.Model;
using Flumine.Util;

namespace Flumine.Model
{
    /// <summary>
    /// Represents worker node
    /// </summary>
    public class Node
    {
        private static readonly ILog Log = Logger.GetLoggerForDeclaringType();

        private readonly INodeApi api;

        private readonly FlumineHostConfig config;

        public NodeDescriptor Descriptor { get; private set; }

        public Guid Id { get; private set; }

        public List<int> AssignedShares { get; private set; }

        public string Endpoint { get; private set; }

        public DateTime LastSeen { get; private set; }

        public int SharesCount
        {
            get
            {
                return AssignedShares.Count;
            }
        }

        public bool IsDead
        {
            get
            {
                return LastSeen.AddMilliseconds(config.DeadNodeTimeout) < DateTime.UtcNow;
            }
        }

        public Node(NodeDescriptor descriptor, INodeApi api, FlumineHostConfig config)
        {
            this.api = api;
            this.config = config;
            Descriptor = descriptor;
            Id = descriptor.NodeId;
            Endpoint = descriptor.Endpoint;
            AssignedShares = new List<int>();
            LastSeen = DateTime.UtcNow;
        }

        public void RefreshState()
        {
            try
            {
                var state = api.GetState();
                if (state.NodeId != Id)
                {
                    // Different node id reported in state. That means that node record is obsolete.
                    LastSeen = DateTime.UtcNow.AddDays(-1);
                    return;
                }

                LastSeen = DateTime.UtcNow;
                AssignedShares = new List<int>(state.AssignedShares ?? Enumerable.Empty<int>());
            }
            catch
            {
                Log.DebugFormat("Failed to contact node {0}", Id.ToString("n"));
            }
        }

        public void AssignShares(IEnumerable<int> shares)
        {
            var shareAssignment = new ShareAssignmentArgs(config.NodeId, shares);
            api.AssignShares(shareAssignment);
            foreach (var shareId in shareAssignment.Shares)
            {
                if (!AssignedShares.Contains(shareId))
                {
                    AssignedShares.Add(shareId);
                }
            }
        }

        public List<int> ReleaseShares(int count)
        {
            // Try to keep oldest assignments at this node.
            List<int> res = AssignedShares.AsEnumerable().Reverse().Take(count).ToList();
            if (res.Count > 0)
            {
                api.ReleaseShares(new ShareAssignmentArgs(config.NodeId, res));
                AssignedShares.RemoveRange(AssignedShares.Count - res.Count, res.Count);
            }

            return res;
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}] shares: [{2}]", Id.ToString("n"), Endpoint, string.Join(",", AssignedShares));
        }
    }
}
