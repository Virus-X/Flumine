using System;
using Flumine.Util;

namespace Flumine.Mongodb
{
    public class NtpServerClockProvider : IServerClockProvider
    {
        private readonly string ntpServer;

        public NtpServerClockProvider()
            : this(NtpClient.DefaultNtpServer)
        {

        }

        public NtpServerClockProvider(string ntpServer)
        {
            this.ntpServer = ntpServer;
        }

        public DateTime GetServerUtc()
        {
            return NtpClient.GetNetworkTime(ntpServer);
        }
    }
}
