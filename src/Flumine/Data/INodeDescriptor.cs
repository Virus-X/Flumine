using System;

namespace Flumine.Data
{
    public interface INodeDescriptor
    {
        Guid NodeId { get; }

        string[] Endpoints { get; }

        DateTime LastSeenAt { get; }
    }
}