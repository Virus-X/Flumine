using Flumine.Util;
using System;

namespace Flumine
{
    public class FlumineHostConfig
    {
        /// <summary>
        /// Gets the rest API endpoint of Flume host.
        /// </summary>
        public string Endpoint { get; private set; }

        /// <summary>
        /// Gets the port range start for dynamic endpoint configuration.
        /// </summary>
        public int PortStart { get; private set; }

        /// <summary>
        /// Gets the port range end for dynamic endpoint configuration.
        /// </summary>
        public int PortEnd { get; private set; }

        /// <summary>
        /// Gets the total count of shares to distribute.
        /// </summary>
        public int SharesCount { get; private set; }

        /// <summary>
        /// Gets or sets the interval of keep alive messages in milliseconds.
        /// Default: 5000 ms.
        /// </summary>
        public int KeepAliveInterval { get; set; }

        /// <summary>
        /// Gets or sets the count of milliseconds since last successfull keep alive to wait before assuming node dead.
        /// Default: 15000 ms.
        /// </summary>
        public int DeadNodeTimeout { get; set; }

        public IServerClockProvider ServerClockProvider { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlumineHostConfig"/> class.
        /// </summary>
        /// <param name="endpoint">Fixed API endpoint of Flume host to use.</param>
        /// <param name="sharesCount">Total count of shares to distribute.</param>
        public FlumineHostConfig(string endpoint, int sharesCount)
        {
            Endpoint = endpoint;
            SharesCount = sharesCount;
            KeepAliveInterval = 5000;
            DeadNodeTimeout = 15000;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlumineHostConfig"/> class.
        /// </summary>
        /// <param name="portStart"> The port range start for dynamic endpoint. </param>
        /// <param name="portEnd"> The port range end for dynamic endpoint. </param>
        /// <param name="sharesCount"> Total count of shares to distribute. </param>
        public FlumineHostConfig(short portStart, short portEnd, int sharesCount)
        {
            if (portEnd < portStart)
            {
                throw new ArgumentException("Negative port range");
            }

            if (portStart == 0)
            {
                throw new ArgumentException("Port cannot be 0");
            }

            PortStart = portStart;
            PortEnd = portEnd;
            SharesCount = sharesCount;
            KeepAliveInterval = 5000;
            DeadNodeTimeout = 15000;
        }
    }
}