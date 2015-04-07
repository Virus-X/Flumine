using System;

using Flumine.Api;
using Flumine.Data;
using Flumine.Util;

namespace Flumine.Model
{
    public class MasterNode : NodeDescriptor
    {
        private static readonly ILog Log = Logger.GetLoggerForDeclaringType();

        private readonly IMasterApi api;

        public MasterNode(INodeDescriptor descriptor, IMasterApi api, FlumineHostConfig config)
            : base(descriptor, config)
        {            
            this.api = api;            
        }

        public void NotifyStartup(NodeDescriptor node)
        {
            api.NotifyStartup(node);
        }

        public void NotifyShutdown(NodeDescriptor node)
        {
            api.NotifyShutdown(node);
        }

        public bool CheckIsAlive()
        {
            try
            {
                api.IsAlive();
                LastSeen = DateTime.UtcNow;
            }
            catch (Exception)
            {
                Log.DebugFormat("Failed to receive response from master");
            }

            return !IsDead;
        }
    }
}