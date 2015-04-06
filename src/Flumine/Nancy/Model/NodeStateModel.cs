using System;
using System.Collections.Generic;
using System.Linq;
using Flumine.Model;

namespace Flumine.Nancy.Model
{
    public class NodeStateModel
    {
        public Guid NodeId { get; set; }

        public List<int> AssignedShares { get; set; }

        public string Endpoint { get; set; }

        public NodeStateModel()
        {            
        }

        public NodeStateModel(NodeDescriptor node)
        {
            NodeId = node.NodeId;
            AssignedShares = node.AssignedShares.ToList();
            Endpoint = node.Endpoint;
        }

        public NodeDescriptor ToNodeDescriptor()
        {
            return new NodeDescriptor
            {
                Endpoint = Endpoint,
                NodeId = NodeId,
                AssignedShares = new List<int>(AssignedShares ?? Enumerable.Empty<int>())
            };
        }
    }
}