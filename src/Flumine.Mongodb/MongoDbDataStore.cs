using System;
using System.Collections.Generic;
using System.Linq;
using Flumine.Data;
using Flumine.Util;
using MongoDB.Driver;

namespace Flumine.Mongodb
{
    public class MongoDbDataStore : IDataStore
    {
        private const string MasterId = "MASTER";

        private readonly IMongoCollection<NodeDescriptorEntity> collection;

        private Guid masterId;

        public MongoDbDataStore(IMongoDatabase db, string collectionName)
        {
            collection = db.GetCollection<NodeDescriptorEntity>(collectionName);
        }

        public INodeDescriptor GetMaster()
        {
            return collection.Find(x => x.Id == MasterId).FirstOrDefault();
        }

        public bool TryTakeMasterRole(INodeDescriptor node, int deadNodeTimeout)
        {
            var deadNodeTs = ServerClock.ServerUtcNow.AddMilliseconds(-deadNodeTimeout);

            var filter = Builders<NodeDescriptorEntity>.Filter;

            var q = filter.And(
                filter.Eq(x => x.Id, MasterId),
                filter.Lt(x => x.LastSeen, deadNodeTs));

            var update = Builders<NodeDescriptorEntity>.Update
                .Set(x => x.NodeId, node.NodeId)
                .Set(x => x.Endpoints, node.Endpoints)
                .Set(x => x.LastSeen, ServerClock.ServerUtcNow);

            try
            {
                var res = collection.FindOneAndUpdate(q, update,
                    new FindOneAndUpdateOptions<NodeDescriptorEntity, NodeDescriptorEntity>
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
            var filter = Builders<NodeDescriptorEntity>.Filter;
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
                Builders<NodeDescriptorEntity>.Update.Set(x => x.LastSeen, now));

            if (node.NodeId == masterId)
            {
                collection.UpdateOne(
                    x => x.Id == MasterId,
                    Builders<NodeDescriptorEntity>.Update.Set(x => x.LastSeen, now));
            }
        }

        public List<INodeDescriptor> GetAllNodes()
        {
            return collection.Find(x => x.Id != MasterId)
                .ToList()
                .Cast<INodeDescriptor>()
                .ToList();
        }

        public void Add(INodeDescriptor node)
        {
            collection.InsertOne(new NodeDescriptorEntity(node));
            RefreshLastSeen(node);
        }

        public void Remove(INodeDescriptor node)
        {
            collection.DeleteOne(x => x.Id == node.NodeId.ToString("n"));
        }
    }
}
