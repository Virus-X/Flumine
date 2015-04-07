using System;

namespace Flumine.Data
{
    public interface INodeDescriptor
    {
        Guid NodeId { get; }

        string Endpoint { get; }

        DateTime LastSeen { get; }
    }
}