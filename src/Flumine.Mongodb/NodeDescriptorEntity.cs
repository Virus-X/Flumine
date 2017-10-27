using System;
using System.Collections.Generic;
using Flumine.Data;
using MongoDB.Bson.Serialization.Attributes;

namespace Flumine.Mongodb
{
    [BsonIgnoreExtraElements]
    public class NodeDescriptorEntity : INodeDescriptor
    {
        public string Id { get; set; }

        public Guid NodeId { get; set; }

        public List<string> Endpoints { get; set; }

        /// <summary>
        /// Note: server time. Use ServerClock for comparison.
        /// </summary>
        public DateTime LastSeen { get; set; }

        public NodeDescriptorEntity()
        {
        }

        public NodeDescriptorEntity(INodeDescriptor descriptor)
        {
            Id = GetId(descriptor.NodeId);
            NodeId = descriptor.NodeId;
            Endpoints = descriptor.Endpoints;
            LastSeen = descriptor.LastSeen;
        }

        public static string GetId(Guid nodeId)
        {
            return nodeId.ToString("n");
        }
    }
}