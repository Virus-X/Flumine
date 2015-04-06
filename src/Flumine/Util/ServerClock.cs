using System;

namespace Flumine.Util
{
    /// <summary>
    /// Represents a clock, synchronized with central server.
    /// While method is very primitive, it can reduce time difference to 1-2 seconds, which is acceptable for Flumine to work.    
    /// </summary>
    public static class ServerClock
    {
        private static readonly ILog Log = Logger.GetLoggerForDeclaringType();

        /// <summary>
        /// Difference in server and client times (Server - Client).
        /// Default = 0.
        /// </summary>
        public static TimeSpan ClockDiff { get; private set; }

        static ServerClock()
        {
            ClockDiff = new TimeSpan(0);
        }

        public static DateTime ServerUtcNow
        {
            get { return ToServerUtc(DateTime.UtcNow); }
        }

        public static DateTime ToLocalUtc(DateTime serverUtc)
        {
            return serverUtc.Add(ClockDiff.Negate());
        }

        public static DateTime ToServerUtc(DateTime localUtc)
        {
            return localUtc.Add(ClockDiff);
        }

        public static void Sync(IServerClockProvider provider, int iterationsCount = 10)
        {
            var bestDiff = provider.GetServerUtc().Subtract(DateTime.UtcNow);
            for (int i = 0; i < iterationsCount - 1; i++)
            {
                var diff = provider.GetServerUtc().Subtract(DateTime.UtcNow);                
                if (Math.Abs(diff.TotalMilliseconds) < Math.Abs(bestDiff.TotalMilliseconds))
                {                    
                    bestDiff = diff;
                }
            }

            Log.DebugFormat("Clock sync finished. Diff: {0}", bestDiff);
            ClockDiff = bestDiff;
        }
    }
}
