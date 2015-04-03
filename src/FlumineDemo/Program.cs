using System;
using System.Configuration;

using Flumine;
using Flumine.Mongodb;
using Flumine.Util;

using MongoDB.Driver;

namespace FlumineDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new FlumineHostConfig("http://10.100.1.32:18081", 10);
            var mongoUrl = MongoUrl.Create(ConfigurationManager.ConnectionStrings["mongo"].ConnectionString);
            var db = new MongoClient(mongoUrl).GetServer().GetDatabase(mongoUrl.DatabaseName);

            // Highly recommended to sync internal clocks with central server or database
            ServerClock.Sync(new MongoDbServerClockProvider(db));

            using (var host = new FlumineHost(config, new MongoDbDataStore(db, "Flumine"), new Worker()))
            {
                host.Start();
                Console.WriteLine("Flumine host started");
                Console.WriteLine("Press enter to stop");
                Console.ReadLine();
            }
        }
    }
}
