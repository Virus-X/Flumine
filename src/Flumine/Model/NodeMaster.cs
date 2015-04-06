using System;

using Flumine.Api;
using Flumine.Util;

namespace Flumine.Model
{
    public class NodeMaster
    {
        private static readonly ILog Log = Logger.GetLoggerForDeclaringType();

        private readonly NodeDescriptor descriptor;

        private readonly IMasterApi api;

        private DateTime lastSeen;

        public bool IsLocal { get; private set; }

        public NodeMaster(NodeDescriptor descriptor, IMasterApi api, bool isLocal)
        {
            lastSeen = ServerClock.ToLocalUtc(descriptor.LastSeenAt);
            this.descriptor = descriptor;
            this.api = api;            
            IsLocal = isLocal;
        }

        public void NotifyStartup(NodeDescriptor node)
        {
            api.NotifyStartup(node);
        }

        public void NotifyShutdown(NodeDescriptor node)
        {
            api.NotifyShutdown(node);
        }

        public bool IsDead(int deadNodeTimeout)
        {
            return lastSeen.AddMilliseconds(deadNodeTimeout) < DateTime.UtcNow;
        }

        public bool CheckIsAlive(int deadNodeTimeout)
        {
            try
            {
                api.IsAlive();
                lastSeen = DateTime.UtcNow;
            }
            catch (Exception)
            {
                Log.DebugFormat("Failed to receive response from master");
            }

            return !IsDead(deadNodeTimeout);
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}]", descriptor.NodeId.ToString("n"), descriptor.Endpoint);
        }
    }
}