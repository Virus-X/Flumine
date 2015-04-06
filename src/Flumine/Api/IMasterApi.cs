using Flumine.Model;

namespace Flumine.Api
{
    public interface IMasterApi
    {
        void NotifyStartup(NodeDescriptor node);

        void NotifyShutdown(NodeDescriptor node);

        bool IsAlive();
    }
}