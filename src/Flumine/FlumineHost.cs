using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        private readonly IDataStore dataStore;
        private readonly IFlumineWorker worker;

        private readonly SingleEntryTimer lastSeenTimer;

        private readonly NancyBootstraper bootstraper;

        private DateTime lastServerTimeSync;

        private MasterNode masterNode;
        private NancyHost nancyHost;
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

        public FlumineHostConfig Config { get; private set; }

        public NodeDescriptor LocalNode { get; private set; }

        public FlumineHost(FlumineHostConfig config, IDataStore dataStore, IFlumineWorker worker)
        {
            Config = config;
            this.dataStore = dataStore;
            this.worker = worker;
            lastSeenTimer = new SingleEntryTimer(OnLastSeenTimerTick, config.KeepAliveInterval);
            bootstraper = new NancyBootstraper(this);
        }

        #region Startup / Shutdown
        public void Start()
        {
            if (!isRunning)
            {
                isRunning = true;
                StartNancy();
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
            return LocalNode;
        }

        public void AssignShares(ShareAssignmentArgs shareAssignment)
        {
            Log.DebugFormat("Allocating shares [{0}]", string.Join(",", shareAssignment.Shares));
            worker.AllocateShares(shareAssignment.Shares);
            LocalNode.AddShares(shareAssignment.Shares);
            Log.InfoFormat("Current shares: [{0}]", string.Join(",", LocalNode.AssignedShares));
        }

        public void ReleaseShares(ShareAssignmentArgs shareAssignment)
        {
            Log.DebugFormat("Releasing shares [{0}]", string.Join(",", shareAssignment.Shares));
            worker.ReleaseShares(shareAssignment.Shares);
            LocalNode.RemoveShares(shareAssignment.Shares);
            Log.InfoFormat("Current shares: [{0}]", string.Join(",", LocalNode.AssignedShares));
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
            if (Config.ServerClockProvider != null && lastServerTimeSync.AddHours(12) < DateTime.UtcNow)
            {
                ServerClock.Sync(Config.ServerClockProvider);
                lastServerTimeSync = DateTime.UtcNow;
            }

            dataStore.RefreshLastSeen(LocalNode);

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
                    var master = new MasterNode(node, new ApiClient(node), Config);
                    if (!master.IsDead)
                    {
                        return master;
                    }
                }

                if (dataStore.TryTakeMasterRole(LocalNode, Config.DeadNodeTimeout))
                {
                    masterService = new FlumineMaster(LocalNode.NodeId, this, dataStore);
                    var master = new MasterNode(LocalNode, masterService, Config);
                    Log.DebugFormat("No master found. Declared myself master.");
                    OnMasterStarted();
                    return master;
                }

                Thread.Sleep(1000);
            }
        }

        private void JoinCluster()
        {
            Log.InfoFormat("{0} is joining cluster", LocalNode);

            dataStore.Add(LocalNode);
            masterNode = GetNodeMaster();

            try
            {
                masterNode.NotifyStartup(LocalNode);
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
                GetNodeMaster().NotifyShutdown(LocalNode);
            }
            catch (Exception ex)
            {
                Log.WarnFormat("Failed to notify about node shutdown: {0}", ex.Message);
            }

            dataStore.Remove(LocalNode);

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

        private void StartNancy()
        {
            if (!string.IsNullOrEmpty(Config.Endpoint))
            {
                Log.InfoFormat("Using endpoint: {0}", Config.Endpoint);
                nancyHost = new NancyHost(new Uri(Config.Endpoint), bootstraper);
                LocalNode = new NodeDescriptor(Guid.NewGuid(), new List<string> { Config.Endpoint }, Config, true);
                nancyHost.Start();
                return;
            }

            Log.InfoFormat("Starting dynamic endpoint allocation in port range {0}-{1}", Config.PortStart, Config.PortEnd);
            for (int port = Config.PortStart; port <= Config.PortEnd; port++)
            {
                try
                {
                    nancyHost = new NancyHost(new Uri("http://localhost:" + port + "/"), bootstraper);
                    nancyHost.Start();
                    Log.InfoFormat("Successfull startup with port {0}", port);
                    LocalNode = new NodeDescriptor(Guid.NewGuid(), GenerateEnpointDefinitions(port), Config, true);
                    return;
                }
                catch (Exception ex)
                {
                    Log.DebugFormat("Cannot bind to port {0}: {1}", port, ex.Message);
                    if (nancyHost != null)
                    {
                        nancyHost.Stop();
                        nancyHost = null;
                    }
                }
            }

            throw new HttpListenerException(0, "Failed to bind to any of provided ports");
        }

        private List<string> GenerateEnpointDefinitions(int port)
        {
            var ips = Dns.GetHostEntry(Dns.GetHostName())
                    .AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .Select(x => x.ToString());

            List<string> endpoints = new List<string>();
            foreach (var ip in ips)
            {
                var endpoint = string.Format("http://{0}:{1}/", ip, port);
                endpoints.Add(endpoint);
                Log.InfoFormat("Discovered endpoint address: {0}", endpoint);
            }

            return endpoints;
        }
    }
}
