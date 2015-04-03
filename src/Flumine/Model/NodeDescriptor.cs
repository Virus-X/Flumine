using System;
using System.Collections.Generic;

using Flumine.Data;
using Flumine.Util;

namespace Flumine.Model
{
    public class NodeDescriptor : INodeDescriptor
    {
        public Guid Id { get; set; }

        public Guid NodeId
        {
            get { return Id; }
        }

        public string Endpoint { get; set; }

        public List<int> AssignedShares { get; set; }

        public DateTime LastSeenAt { get; set; }

        public NodeDescriptor()
        {
            LastSeenAt = DateTime.UtcNow;
            AssignedShares = new List<int>();
        }

        public NodeDescriptor(INodeDescriptor descriptor)
            : this()
        {
            Id = descriptor.NodeId;
            Endpoint = descriptor.Endpoint;
            LastSeenAt = descriptor.LastSeenAt;
        }

        public NodeDescriptor(FlumineHostConfig config)
            : this()
        {
            Id = config.NodeId;
            Endpoint = config.Endpoint;
        }

        public bool IsDead(int deadNodeTimeout)
        {
            return LastSeenAt.AddMilliseconds(deadNodeTimeout) < ServerClock.ServerUtcNow;
        }
    }
}