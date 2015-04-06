using System;
using System.Collections.Generic;

using Flumine.Data;
using Flumine.Util;

namespace Flumine.Model
{
    public class NodeDescriptor : INodeDescriptor
    {
        public Guid NodeId { get; set; }

        public string Endpoint { get; set; }

        public DateTime LastSeenAt { get; set; }

        public List<int> AssignedShares { get; set; }

        public NodeDescriptor()
        {
            LastSeenAt = DateTime.UtcNow;
            AssignedShares = new List<int>();
        }

        public NodeDescriptor(INodeDescriptor descriptor)
            : this()
        {
            NodeId = descriptor.NodeId;
            Endpoint = descriptor.Endpoint;
            LastSeenAt = descriptor.LastSeenAt;
        }

        public NodeDescriptor(FlumineHostConfig config)
            : this()
        {
            NodeId = config.NodeId;
            Endpoint = config.Endpoint;
        }

        public bool IsDead(int deadNodeTimeout)
        {
            return LastSeenAt.AddMilliseconds(deadNodeTimeout) < ServerClock.ServerUtcNow;
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}]", NodeId.ToString("n"), Endpoint);
        }

        public void AddShares(IEnumerable<int> shareIds)
        {
            foreach (var id in shareIds)
            {
                AssignedShares.Add(id);
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