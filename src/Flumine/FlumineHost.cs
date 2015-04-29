using System;
using System.Threading;

using Flumine.Api;
using Flumine.Data;
using Flumine.Model;
using Flumine.Util;

using Nancy.Hosting.Self;

namespace Flumine
{
    public class FlumineHost : IDisposable, INodeApi
    {
        private static readonly ILog Log = Logger.GetLoggerForDeclaringType();

        private readonly FlumineHostConfig config;
        private readonly IDataStore dataStore;
        private readonly IFlumineWorker worker;
        private readonly NancyHost nancyHost;

        private readonly NodeDescriptor currentNode;
        private readonly SingleEntryTimer lastSeenTimer;

        private MasterNode masterNode;

        private bool isRunning;

        private FlumineMaster masterService;

        /// <summary>
        /// Fires when current host becomes master.
        /// </summary>
        public event EventHandler MasterStarted;

        /// <summary>
        /// Fires when current host leaves master role.
        /// </summary>
        public event EventHandler MasterStopped;

        public FlumineHostConfig Config
        {
            get { return config; }
        }

        public INodeDescriptor LocalNode
        {
            get { return currentNode; }
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
            Log.InfoFormat("Current shares: [{0}]", string.Join(",", currentNode.AssignedShares));
        }

        public void ReleaseShares(ShareAssignmentArgs shareAssignment)
        {
            Log.DebugFormat("Releasing shares [{0}]", string.Join(",", shareAssignment.Shares));
            worker.ReleaseShares(shareAssignment.Shares);
            currentNode.RemoveShares(shareAssignment.Shares);
            Log.InfoFormat("Current shares: [{0}]", string.Join(",", currentNode.AssignedShares));
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

        #endregion

        public void Dispose()
        {
            Stop();
            nancyHost.Dispose();
            lastSeenTimer.Dispose();

            if (masterService != null)
            {
                masterService.Dispose();
                OnMasterStopped();
            }
        }

        private void OnLastSeenTimerTick(object state)
        {
            dataStore.RefreshLastSeen(currentNode);

            if (!masterNode.CheckIsAlive())
            {
                Log.DebugFormat("Master node is dead");
                masterNode = GetNodeMaster();
                Log.DebugFormat("Master is {0}", masterNode);
            }
        }

        private MasterNode GetNodeMaster()
        {
            while (true)
            {
                var node = dataStore.GetMaster();
                if (node != null)
                {
                    var master = new MasterNode(node, new ApiClient(node), config);
                    if (!master.IsDead)
                    {
                        return master;
                    }
                }

                if (dataStore.TryTakeMasterRole(currentNode, config.DeadNodeTimeout))
                {
                    masterService = new FlumineMaster(this, dataStore);
                    var master = new MasterNode(currentNode, masterService, config);
                    Log.DebugFormat("No master found. Declared myself master.");
                    OnMasterStarted();
                    return master;
                }

                Thread.Sleep(1000);
            }
        }

        private void JoinCluster()
        {
            Log.InfoFormat("{0} is joining cluster", currentNode);

            dataStore.Add(currentNode);
            masterNode = GetNodeMaster();

            try
            {
                masterNode.NotifyStartup(currentNode);
            }
            catch (Exception ex)
            {
                Log.WarnFormat("Failed to notify about node startup: {0}", ex.Message);
            }

            Log.DebugFormat("Joined cluster. Master is {0}", masterNode);
        }

        private void LeaveCluster()
        {
            try
            {
                GetNodeMaster().NotifyShutdown(currentNode);
            }
            catch (Exception ex)
            {
                Log.WarnFormat("Failed to notify about node shutdown: {0}", ex.Message);
            }

            dataStore.Remove(currentNode);

            if (masterService != null)
            {
                masterService.Dispose();
                masterService = null;
                OnMasterStopped();
            }
        }

        private void OnMasterStarted()
        {
            var ev = MasterStarted;
            if (ev != null)
            {
                ev(this, EventArgs.Empty);
            }
        }

        private void OnMasterStopped()
        {
            var ev = MasterStopped;
            if (ev != null)
            {
                ev(this, EventArgs.Empty);
            }
        }
    }
}
