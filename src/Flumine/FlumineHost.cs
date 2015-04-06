using System;
using System.Threading;

using Flumine.Api;
using Flumine.Data;
using Flumine.Model;
using Flumine.Nancy;
using Flumine.Nancy.Model;
using Flumine.Util;

using Nancy.Hosting.Self;

namespace Flumine
{
    public class FlumineHost : IDisposable, INodeApi, IMasterApi
    {
        private static readonly ILog Log = Logger.GetLoggerForDeclaringType();

        private readonly FlumineHostConfig config;
        private readonly IDataStore dataStore;
        private readonly IFlumineWorker worker;
        private readonly NancyHost nancyHost;

        private readonly NodeDescriptor currentNode;
        private readonly SingleEntryTimer lastSeenTimer;

        private NodeMaster nodeMaster;

        private bool isRunning;

        private FlumineMaster masterService;

        public FlumineHostConfig Config
        {
            get { return config; }
        }

        public FlumineHost(FlumineHostConfig config, IDataStore dataStore, IFlumineWorker worker)
        {
            this.config = config;
            this.dataStore = dataStore;
            this.worker = worker;
            nancyHost = new NancyHost(new Uri(config.Endpoint), new NancyBootstraper(this));
            currentNode = new NodeDescriptor(config);
            lastSeenTimer = new SingleEntryTimer(OnLastSeenTimerTick, config.KeepAliveInterval);
        }

        #region Startup / Shutdown
        public void Start()
        {
            if (!isRunning)
            {
                isRunning = true;
                nancyHost.Start();
                JoinCluster();
                lastSeenTimer.Start();
            }
        }

        public void Stop()
        {
            if (isRunning)
            {
                isRunning = false;
                lastSeenTimer.Stop();
                LeaveCluster();
                nancyHost.Stop();
            }
        }
        #endregion

        #region API

        public NodeDescriptor GetState()
        {
            return currentNode;
        }

        public void AssignShares(ShareAssignmentArgs shareAssignment)
        {
            Log.DebugFormat("Allocating shares [{0}]", string.Join(",", shareAssignment.Shares));
            worker.AllocateShares(shareAssignment.Shares);
            currentNode.AddShares(shareAssignment.Shares);
            Log.DebugFormat("Current shares: {0}", string.Join(",", currentNode.AssignedShares));
        }

        public void ReleaseShares(ShareAssignmentArgs shareAssignment)
        {
            Log.DebugFormat("Releasing shares [{0}]", string.Join(",", shareAssignment.Shares));
            worker.ReleaseShares(shareAssignment.Shares);
            currentNode.RemoveShares(shareAssignment.Shares);
            Log.DebugFormat("Current shares: {0}", string.Join(",", currentNode.AssignedShares));
        }

        public void NotifyStartup(NodeDescriptor node)
        {
            if (masterService == null)
            {
                throw new InvalidOperationException("Not a master");
            }

            masterService.NotifyStartup(node);
        }

        public void NotifyShutdown(NodeDescriptor node)
        {
            if (masterService == null)
            {
                throw new InvalidOperationException("Not a master");
            }

            masterService.NotifyShutdown(node);
        }

        public bool IsAlive()
        {
            return true;
        }

        #endregion

        public void Dispose()
        {
            Stop();
            nancyHost.Dispose();
            lastSeenTimer.Dispose();
            masterService.Dispose();
        }

        private void OnLastSeenTimerTick(object state)
        {
            dataStore.RefreshLastSeen(currentNode);

            if (!nodeMaster.CheckIsAlive(config.DeadNodeTimeout))
            {
                Log.DebugFormat("Master node is dead");
                nodeMaster = GetNodeMaster();
                Log.DebugFormat("Master is {0}", nodeMaster);
            }
        }

        private NodeMaster GetNodeMaster()
        {
            while (true)
            {
                var node = dataStore.GetMaster();
                if (node != null)
                {
                    var master = new NodeMaster(new NodeDescriptor(node), new ApiClient(node.Endpoint), false);
                    if (!master.IsDead(config.DeadNodeTimeout))
                    {
                        return master;
                    }
                }

                if (dataStore.TryTakeMasterRole(currentNode, config.DeadNodeTimeout))
                {
                    masterService = new FlumineMaster(this, dataStore);
                    var master = new NodeMaster(currentNode, masterService, true);
                    Log.DebugFormat("No master found. Declared myself master.");
                    return master;
                }

                Thread.Sleep(1000);
            }
        }

        private void JoinCluster()
        {
            dataStore.Add(currentNode);
            nodeMaster = GetNodeMaster();

            try
            {
                nodeMaster.NotifyStartup(currentNode);
            }
            catch (Exception ex)
            {
                Log.WarnFormat("Failed to notify about node startup: {0}", ex);
            }

            Log.DebugFormat("Joined cluster. Master is {0}", nodeMaster);
        }

        private void LeaveCluster()
        {
            try
            {
                GetNodeMaster().NotifyShutdown(currentNode);
            }
            catch (Exception ex)
            {
                Log.WarnFormat("Failed to notify about node startup: {0}", ex);
            }

            dataStore.Remove(currentNode);

            if (masterService != null)
            {
                masterService.Dispose();
                masterService = null;
            }
        }
    }
}
