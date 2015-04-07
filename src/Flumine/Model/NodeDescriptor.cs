using System;
using System.Collections.Generic;

using Flumine.Data;

namespace Flumine.Model
{
    public class NodeDescriptor : INodeDescriptor
    {
        private readonly FlumineHostConfig config;

        public Guid NodeId { get; protected set; }

        public string Endpoint { get; protected set; }

        public DateTime LastSeen { get; protected set; }

        public List<int> AssignedShares { get; protected set; }

        public bool IsDead
        {
            get
            {
                return LastSeen.AddMilliseconds(config.DeadNodeTimeout) < DateTime.UtcNow;
            }
        }

        public NodeDescriptor()
        {
            LastSeen = DateTime.UtcNow;
            AssignedShares = new List<int>();
        }

        public NodeDescriptor(FlumineHostConfig config)
            : this()
        {
            this.config = config;
            NodeId = config.NodeId;
            Endpoint = config.Endpoint;
        }

        public NodeDescriptor(INodeDescriptor descriptor, FlumineHostConfig config)
            : this()
        {
            this.config = config;
            NodeId = descriptor.NodeId;
            Endpoint = descriptor.Endpoint;
            LastSeen = descriptor.LastSeen;
        }

        public override string ToString()
        {
            if (AssignedShares != null && AssignedShares.Count > 0)
            {
                return string.Format("{0} [{1}] shares: [{2}]", NodeId.ToString("n").Remove(8), Endpoint, string.Join(",", AssignedShares));
            }

            return string.Format("{0} [{1}]", NodeId.ToString("n").Remove(8), Endpoint);
        }

        public void AddShares(IEnumerable<int> shareIds)
        {
            foreach (var id in shareIds)
            {
                if (!AssignedShares.Contains(id))
                {
                    AssignedShares.Add(id);
                }
            }
        }

        public void RemoveShares(IEnumerable<int> shareIds)
        {
            foreach (var id in shareIds)
            {
                AssignedShares.Remove(id);
            }
        }
    }
}