using System;
using System.Collections.Generic;

namespace Flumine.Data
{
    public interface INodeDescriptor
    {
        Guid NodeId { get; }

        List<string> Endpoints { get; }

        DateTime LastSeen { get; }
    }
}