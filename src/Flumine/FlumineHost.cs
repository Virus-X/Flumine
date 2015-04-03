using System;
using System.Collections.Generic;
using System.Threading;

using Flumine.Data;
using Flumine.Model;
using Flumine.Nancy;

using Nancy.Hosting.Self;

namespace Flumine
{
    public class FlumineHost : IDisposable
    {
        private readonly FlumineHostConfig config;
        private readonly IDataStore dataStore;
        private readonly IFlumineWorker worker;
        private readonly NancyHost nancyHost;

        private readonly NodeDescriptor currentNode;
        private readonly Timer lastSeenTimer;

        private bool isRunning;

        private NodeDescriptor masterNode;

        public FlumineHost(FlumineHostConfig config, IDataStore dataStore, IFlumineWorker worker)
        {
            this.config = config;
            this.dataStore = dataStore;
            this.worker = worker;
            nancyHost = new NancyHost(new Uri(config.Endpoint), new NancyBootstraper(this));
            currentNode = new NodeDescriptor(config);
            lastSeenTimer = new Timer(OnLastSeenTimerTick);
        }

        #region Startup / Shutdown
        public void Start()
        {
            if (!isRunning)
            {
                isRunning = true;
                nancyHost.Start();
                dataStore.Add(currentNode);
                lastSeenTimer.Change(config.KeepAliveInterval, config.KeepAliveInterval);
            }
        }

        public void Stop()
        {
            if (isRunning)
            {
                nancyHost.Stop();
                dataStore.Remove(currentNode);
                lastSeenTimer.Change(Timeout.Infinite, Timeout.Infinite);
                isRunning = false;
            }
        }
        #endregion

        #region API

        public NodeDescriptor GetState()
        {
            return currentNode;
        }

        public void AssignShares(IEnumerable<int> shares)
        {
            worker.AllocateShares(shares);
        }

        public void ReleaseShares(IEnumerable<int> shares)
        {
            worker.ReleaseShares(shares);
        }

        public void ProcessShutdownNotification(Guid node, IEnumerable<int> ownedShares)
        {

        }

        public void ProcessStartupNotification(Guid node, string endpoint)
        {

        }

        #endregion

        public void Dispose()
        {
            Stop();
            nancyHost.Dispose();
            lastSeenTimer.Dispose();
        }

        private void OnLastSeenTimerTick(object state)
        {
            dataStore.RefreshLastSeen(currentNode);
            if (masterNode == null)
            {
                ResolveMaster();
            }
        }

        private void ResolveMaster()
        {
            while (masterNode == null)
            {
                var master = dataStore.GetMaster();

                if (master == null || new NodeDescriptor(master).IsDead(config.DeadNodeTimeout))
                {
                    // Master is dead?
                    if (dataStore.TryTakeMasterRole(currentNode, config.DeadNodeTimeout))
                    {
                        // Took master role!
                        masterNode = currentNode;
                        return;
                    }
                }
                else
                {
                    masterNode = new NodeDescriptor(master);
                }

                Thread.Sleep(1000);
            }
        }
    }
}
