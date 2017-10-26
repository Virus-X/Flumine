using System;
using System.Net;
using System.Net.Sockets;

namespace Flumine.Util
{
    public static class NtpClient
    {
        public const string DefaultNtpServer = "pool.ntp.org";

        public static DateTime GetNetworkTime(string ntpServer = DefaultNtpServer)
        {
            var ntpData = new byte[48];
            ntpData[0] = 0x1B; //LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveTimeout = 3000,
                SendTimeout = 3000
            };

            socket.Connect(ipEndPoint);
            socket.Send(ntpData);
            socket.Receive(ntpData);
            socket.Close();

            ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | (ulong)ntpData[43];
            ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | (ulong)ntpData[47];

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
            var networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);

            return networkDateTime;
        }

        public static bool TryGetNetworkTime(out DateTime currentTime, string ntpServer = DefaultNtpServer)
        {
            try
            {
                currentTime = GetNetworkTime(ntpServer);
                return true;
            }
            catch
            {
                currentTime = DateTime.UtcNow;
                return false;
            }
        }
    }
}
