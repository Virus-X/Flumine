using System;

using Flumine.Model;

namespace Flumine.Nancy.Model
{
    public class NodeStateModel
    {
        public Guid NodeId { get; set; }

        public int[] AssignedShares { get; set; }

        public string Endpoint { get; set; }

        public NodeStateModel() { }

        public NodeStateModel(NodeDescriptor node)
        {
            NodeId = node.NodeId;
            AssignedShares = node.AssignedShares.ToArray();
            Endpoint = node.Endpoint;
        }
    }
}