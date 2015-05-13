using System;
using System.Collections.Generic;
using System.Linq;

using Flumine.Data;

namespace Flumine.Model
{
    public class NodeDescriptor : INodeDescriptor
    {
        private readonly FlumineHostConfig config;

        public Guid NodeId { get; protected set; }

        public List<string> Endpoints { get; protected set; }

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

        public NodeDescriptor(Guid id, IEnumerable<string> endpoints, FlumineHostConfig config)
            : this()
        {
            this.config = config;
            NodeId = id;
            Endpoints = new List<string>(endpoints ?? Enumerable.Empty<string>());
        }

        public NodeDescriptor(INodeDescriptor descriptor, FlumineHostConfig config)
            : this(descriptor.NodeId, descriptor.Endpoints, config)
        {
            LastSeen = descriptor.LastSeen;
        }

        public override string ToString()
        {
            if (AssignedShares != null && AssignedShares.Count > 0)
            {
                return string.Format("{0} [{1}] shares: [{2}]", NodeId.ToString("n").Remove(8), string.Join(",", Endpoints), string.Join(",", AssignedShares));
            }

            return string.Format("{0} [{1}]", NodeId.ToString("n").Remove(8), string.Join(",", Endpoints));
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