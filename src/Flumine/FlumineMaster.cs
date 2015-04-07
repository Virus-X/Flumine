using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Flumine.Api;
using Flumine.Data;
using Flumine.Model;
using Flumine.Nancy;
using Flumine.Util;

namespace Flumine
{
    internal class FlumineMaster : IDisposable, IMasterApi
    {
        private static readonly ILog Log = Logger.GetLoggerForDeclaringType();

        private readonly FlumineHost host;

        private readonly IDataStore dataStore;

        private readonly Dictionary<Guid, Node> clusterNodes;

        private readonly HashSet<int> freeShares;

        private readonly SingleEntryTimer processorTimer;

        private bool shouldReassignShares;

        private bool masterInitialized;

        public FlumineMaster(FlumineHost host, IDataStore dataStore)
        {
            this.host = host;
            this.dataStore = dataStore;
            clusterNodes = new Dictionary<Guid, Node>();
            freeShares = new HashSet<int>();
            LoadExistingNodes();
            freeShares = new HashSet<int>(Enumerable.Range(0, host.Config.SharesCount));
            processorTimer = new SingleEntryTimer(ProcessorTimerTick, 2000);
            processorTimer.Start();
        }

        public void Dispose()
        {
            dataStore.LeaveMasterRole(host.GetState());
            processorTimer.Dispose();
        }

        public void NotifyStartup(NodeDescriptor node)
        {
            if (!clusterNodes.ContainsKey(node.NodeId))
            {
                clusterNodes[node.NodeId] = new Node(node, new ApiClient(node.Endpoint), host.Config);
            }

            try
            {
                clusterNodes[node.NodeId].RefreshState();
                shouldReassignShares = true;
            }
            catch (Exception ex)
            {
                Log.DebugFormat("Failed to refresh node {0}: {1}", node, ex.Message);
            }
        }

        public void NotifyShutdown(NodeDescriptor node)
        {
            if (!clusterNodes.ContainsKey(node.NodeId))
            {
                clusterNodes.Remove(node.NodeId);

                if (node.AssignedShares.Any())
                {
                    foreach (var share in node.AssignedShares)
                    {
                        freeShares.Add(share);
                    }

                    shouldReassignShares = true;
                }
            }
        }

        public bool IsAlive()
        {
            return true;
        }

        private void ProcessorTimerTick(object state)
        {
            RefreshNodeStates();
            if (masterInitialized && shouldReassignShares)
            {
                ReassignShares();
            }
        }

        private void RefreshNodeStates()
        {
            Parallel.ForEach(
                clusterNodes.Values.ToList(),
                node => node.RefreshState());

            foreach (var node in clusterNodes.Values.Where(x => x.IsDead).ToList())
            {
                if (node.AssignedShares.Any())
                {
                    foreach (var share in node.AssignedShares)
                    {
                        freeShares.Add(share);
                    }
                }

                shouldReassignShares = true;
                Log.DebugFormat("Removing dead node {0}", node);
                clusterNodes.Remove(node.Id);
                dataStore.Remove(node.Descriptor);
            }

            if (!masterInitialized && clusterNodes.Values.All(x => x.StateSynchronized))
            {
                foreach (var node in clusterNodes.Values.ToList())
                {
                    foreach (var share in node.AssignedShares)
                    {
                        freeShares.Remove(share);
                    }
                }

                shouldReassignShares = true;
                masterInitialized = true;
                Log.DebugFormat("Master initialization complete");
            }
        }

        private void ReassignShares()
        {
            var nodes = clusterNodes.Values.ToList();
            if (!nodes.Any())
            {
                Log.Debug("No healthy nodes available");
                return;
            }

            var sharesPerNode = (int)Math.Ceiling((double)host.Config.SharesCount / nodes.Count);

            // 1. Scrap all overloaded nodes
            var overloadedNodes = nodes.Where(x => x.SharesCount > sharesPerNode);
            foreach (var node in overloadedNodes)
            {
                try
                {
                    var shares = node.ReleaseShares(node.SharesCount - sharesPerNode);
                    foreach (var s in shares)
                    {
                        freeShares.Add(s);
                    }
                }
                catch (Exception ex)
                {
                    Log.DebugFormat("Failed to release shares from node {0}: {1}", node, ex.Message);
                }
            }

            if (freeShares.Count == 0)
            {
                shouldReassignShares = false;
                return;
            }

            // 2. Redisribute shares among all nodes
            var underloadedNodes = clusterNodes.Values.Where(x => x.SharesCount < sharesPerNode).OrderBy(x => x.SharesCount).ToList();
            foreach (var node in underloadedNodes)
            {
                try
                {
                    if (freeShares.Count == 0)
                    {
                        return;
                    }

                    var shares = freeShares.Take(sharesPerNode - node.SharesCount).ToList();
                    node.AssignShares(shares);
                    foreach (var s in shares)
                    {
                        freeShares.Remove(s);
                    }
                }
                catch (Exception ex)
                {
                    Log.DebugFormat("Failed to assign shares to node {0}: {1}", node, ex.Message);
                }
            }

            shouldReassignShares = !freeShares.Any();
        }

        private void LoadExistingNodes()
        {
            var hostNode = host.GetState();
            foreach (var node in dataStore.GetAllNodes().Select(x => new NodeDescriptor(x)))
            {
                var value = node.NodeId == hostNode.NodeId
                                 ? new Node(hostNode, host, host.Config)
                                 : new Node(node, new ApiClient(node.Endpoint), host.Config);

                clusterNodes.Add(node.NodeId, value);
            }
        }
    }
}
