using System;

namespace Flumine.Data
{
    public class NodeDescriptor : INodeDescriptor
    {
        public Guid Id { get; set; }

        public Guid NodeId
        {
            get { return Id; }
        }

        public string[] Endpoints { get; set; }

        public int[] AssignedShares { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime LastSeenAt { get; set; }

        public NodeDescriptor()
        {
        }

        public NodeDescriptor(params string[] endpoints)
        {
            Id = Guid.NewGuid();
            Endpoints = endpoints;
            StartedAt = DateTime.UtcNow;
            LastSeenAt = StartedAt;
        }

        public void UpdateLastSeen()
        {
            LastSeenAt = DateTime.UtcNow;
        }
    }
}