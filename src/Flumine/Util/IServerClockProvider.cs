using System;

namespace Flumine.Util
{
    public interface IServerClockProvider
    {
        DateTime GetServerUtc();
    }
}
