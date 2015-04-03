namespace Flumine
{
    public class FlumineHostConfig
    {
        /// <summary>
        /// Gets the rest API endpoint of Flume host.
        /// </summary>
        public string Endpoint { get; private set; }

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
        /// Initializes a new instance of the <see cref="FlumineHostConfig"/> class.
        /// </summary>
        /// <param name="endpoint">API endpoint of Flume host to use.</param>
        /// <param name="sharesCount">Total count of shares to distribute.</param>
        public FlumineHostConfig(string endpoint, int sharesCount)
        {
            Endpoint = endpoint;
            SharesCount = sharesCount;
            KeepAliveInterval = 5000;
        }
    }
}