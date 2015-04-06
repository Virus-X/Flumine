using System;
using System.Collections.Generic;
using System.Linq;

using Flumine.Data;
using Flumine.Util;

using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Flumine.Mongodb
{
    public class MongoDbDataStore : IDataStore
    {
        private const string MasterId = "MASTER";

        private readonly MongoCollection<NodeDescriptor> collection;

        private Guid masterId;

        public MongoDbDataStore(MongoDatabase db, string collectionName)
        {
            collection = db.GetCollection<NodeDescriptor>(collectionName);
        }

        public INodeDescriptor GetMaster()
        {
            return collection.FindOne(GetIdQuery(MasterId));
        }

        public bool TryTakeMasterRole(INodeDescriptor node, int deadNodeTimeout)
        {
            var deadNodeTs = ServerClock.ServerUtcNow.AddMilliseconds(-deadNodeTimeout);

            var q = Query.And(
                GetIdQuery(MasterId),
                Query.Or(
                    Query<NodeDescriptor>.NotExists(x => x.NodeId),
                    Query<NodeDescriptor>.LT(x => x.LastSeenAt, deadNodeTs)));

            var update = Update<NodeDescriptor>
                .Set(x => x.NodeId, node.NodeId)
                .Set(x => x.Endpoint, node.Endpoint)
                .Set(x => x.LastSeenAt, ServerClock.ServerUtcNow);

            var args = new FindAndModifyArgs
            {
                Query = q,
                Update = update,
                Upsert = true
            };

            var res = collection.FindAndModify(args);
            if (res.Ok)
            {
                masterId = node.NodeId;
                return true;
            }

            return false;
        }

        public void LeaveMasterRole(INodeDescriptor node)
        {
            var args = new FindAndRemoveArgs
            {
                Query = Query.And(
                    GetIdQuery(MasterId),
                    Query<NodeDescriptor>.EQ(x => x.NodeId, node.NodeId))
            };

            masterId = Guid.Empty;
            collection.FindAndRemove(args);
        }

        public void RefreshLastSeen(INodeDescriptor node)
        {
            var now = ServerClock.ServerUtcNow;
            collection.Update(
                GetIdQuery(node.NodeId),
                Update<NodeDescriptor>.Set(x => x.LastSeenAt, now));

            if (node.NodeId == masterId)
            {
                collection.Update(
                    GetIdQuery(MasterId),
                    Update<NodeDescriptor>.Set(x => x.LastSeenAt, now));
            }
        }

        public List<INodeDescriptor> GetAllNodes()
        {
            return collection.FindAll()
                .Where(x => x.Id != MasterId)
                .Cast<INodeDescriptor>()
                .ToList();
        }

        public void Add(INodeDescriptor node)
        {
            collection.Insert(new NodeDescriptor(node));
            RefreshLastSeen(node);
        }

        public void Remove(INodeDescriptor node)
        {
            collection.Remove(GetIdQuery(node.NodeId));
        }

        private IMongoQuery GetIdQuery(string id)
        {
            return Query.EQ("_id", id);
        }

        private IMongoQuery GetIdQuery(Guid nodeId)
        {
            return Query.EQ("_id", NodeDescriptor.GetId(nodeId));
        }

        #region Inner types

        private class NodeDescriptor : INodeDescriptor
        {
            public string Id { get; set; }

            public Guid NodeId { get; set; }

            public string Endpoint { get; set; }

            public DateTime LastSeenAt { get; set; }

            public NodeDescriptor()
            {
            }

            public NodeDescriptor(INodeDescriptor descriptor)
            {
                Id = GetId(descriptor.NodeId);
                NodeId = descriptor.NodeId;
                Endpoint = descriptor.Endpoint;
                LastSeenAt = descriptor.LastSeenAt;
            }

            public static string GetId(Guid nodeId)
            {
                return nodeId.ToString("n");
            }
        }

        #endregion
    }
}
