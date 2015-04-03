using System;

using Flumine.Util;

using MongoDB.Driver;

namespace Flumine.Mongodb
{
    public class MongoDbServerClockProvider : IServerClockProvider
    {
        private readonly MongoDatabase db;

        public MongoDbServerClockProvider(MongoDatabase db)
        {
            this.db = db;
        }

        public DateTime GetServerUtc()
        {
            return db.Eval(new EvalArgs { Code = "return new Date()" }).AsBsonDateTime.ToUniversalTime();
        }
    }
}
