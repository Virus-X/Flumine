using System;
using System.Configuration;
using System.IO;
using Flumine;
using Flumine.Mongodb;
using Flumine.Util;
using log4net.Config;

using MongoDB.Driver;

namespace FlumineDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            XmlConfigurator.Configure(new FileInfo("log4net.config"));           
            var config = new FlumineHostConfig(13001, 13010, 8);
            var mongoUrl = MongoUrl.Create(ConfigurationManager.ConnectionStrings["mongo"].ConnectionString);
            var db = new MongoClient(mongoUrl).GetDatabase(mongoUrl.DatabaseName);

            // Highly recommended to sync internal clocks with central server or database
            ServerClock.Sync(new NtpServerClockProvider());

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
