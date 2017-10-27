using System;
using System.Collections.Generic;
using System.Linq;

using Flumine.Data;
using Flumine.Util;

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
                return !IsLocal && LastSeen.AddMilliseconds(config.DeadNodeTimeout) < ServerClock.ServerUtcNow;
            }
        }

        public bool IsLocal { get; private set; }

        public NodeDescriptor()
        {
            MarkAlive();
            AssignedShares = new List<int>();
        }

        public NodeDescriptor(Guid id, IEnumerable<string> endpoints, FlumineHostConfig config, bool isLocalNode)
            : this()
        {
            this.config = config;
            NodeId = id;
            IsLocal = isLocalNode;
            Endpoints = new List<string>(endpoints ?? Enumerable.Empty<string>());
        }

        public NodeDescriptor(INodeDescriptor descriptor, FlumineHostConfig config, bool isLocalNode = false)
            : this(descriptor.NodeId, descriptor.Endpoints, config, isLocalNode)
        {
            LastSeen = descriptor.LastSeen;
        }

        public void MarkAlive()
        {
            LastSeen = ServerClock.ServerUtcNow;
        }

        public void MarkDead()
        {
            LastSeen = DateTime.UtcNow.AddDays(-7);
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