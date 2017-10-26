using System;
using System.Collections.Generic;
using System.Linq;
using Flumine.Data;
using Flumine.Util;

using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Flumine.Mongodb
{
    public class MongoDbDataStore : IDataStore
    {
        private const string MasterId = "MASTER";

        private readonly IMongoCollection<NodeDescriptor> collection;

        private Guid masterId;

        public MongoDbDataStore(IMongoDatabase db, string collectionName)
        {
            collection = db.GetCollection<NodeDescriptor>(collectionName);
        }

        public INodeDescriptor GetMaster()
        {
            var node = collection.Find(x => x.Id == MasterId).FirstOrDefault();
            return node == null ? null : node.ConvertTimeToLocal();
        }

        public bool TryTakeMasterRole(INodeDescriptor node, int deadNodeTimeout)
        {
            var deadNodeTs = ServerClock.ServerUtcNow.AddMilliseconds(-deadNodeTimeout);

            var filter = Builders<NodeDescriptor>.Filter;

            var q = filter.And(
                filter.Eq(x => x.Id, MasterId),
                filter.Or(
                    filter.Exists(x => x.NodeId, false),
                    filter.Lt(x => x.LastSeen, deadNodeTs)));

            var update = Builders<NodeDescriptor>.Update
                .Set(x => x.NodeId, node.NodeId)
                .Set(x => x.Endpoints, node.Endpoints)
                .Set(x => x.LastSeen, ServerClock.ServerUtcNow);

            try
            {
                var res = collection.FindOneAndUpdate(q, update,
                    new FindOneAndUpdateOptions<NodeDescriptor, NodeDescriptor>
                    {
                        IsUpsert = true,
                        ReturnDocument = ReturnDocument.After
                    });

                masterId = node.NodeId;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void LeaveMasterRole(INodeDescriptor node)
        {
            var filter = Builders<NodeDescriptor>.Filter;
            var q = filter.And(
                filter.Eq(x => x.Id, MasterId),
                filter.Eq(x => x.NodeId, node.NodeId));

            masterId = Guid.Empty;
            collection.FindOneAndDelete(q);
        }

        public void RefreshLastSeen(INodeDescriptor node)
        {
            var now = ServerClock.ServerUtcNow;
            collection.UpdateOne(
                x => x.Id == node.NodeId.ToString("n"),
                Builders<NodeDescriptor>.Update.Set(x => x.LastSeen, now));

            if (node.NodeId == masterId)
            {
                collection.UpdateOne(
                    x => x.Id == MasterId,
                    Builders<NodeDescriptor>.Update.Set(x => x.LastSeen, now));
            }
        }

        public List<INodeDescriptor> GetAllNodes()
        {
            return collection.Find(x => x.Id != MasterId)
                .ToList()
                .Select(node => node.ConvertTimeToLocal())
                .Cast<INodeDescriptor>()
                .ToList();
        }

        public void Add(INodeDescriptor node)
        {
            collection.InsertOne(new NodeDescriptor(node));
            RefreshLastSeen(node);
        }

        public void Remove(INodeDescriptor node)
        {
            collection.DeleteOne(x => x.Id == node.NodeId.ToString("n"));
        }

        #region Inner types

        [BsonIgnoreExtraElements]
        private class NodeDescriptor : INodeDescriptor
        {
            private bool isLocalTime;

            public string Id { get; set; }

            public Guid NodeId { get; set; }

            public List<string> Endpoints { get; set; }

            public DateTime LastSeen { get; set; }

            public NodeDescriptor()
            {
            }

            public NodeDescriptor(INodeDescriptor descriptor)
            {
                Id = GetId(descriptor.NodeId);
                NodeId = descriptor.NodeId;
                Endpoints = descriptor.Endpoints;
                LastSeen = descriptor.LastSeen;
            }

            public NodeDescriptor ConvertTimeToLocal()
            {
                if (!isLocalTime)
                {
                    LastSeen = ServerClock.ToLocalUtc(LastSeen);
                    isLocalTime = true;
                }

                return this;
            }

            public NodeDescriptor ConvertTimeToServer()
            {
                if (isLocalTime)
                {
                    LastSeen = ServerClock.ToServerUtc(LastSeen);
                    isLocalTime = false;
                }

                return this;
            }

            public static string GetId(Guid nodeId)
            {
                return nodeId.ToString("n");
            }
        }

        #endregion
    }
}
