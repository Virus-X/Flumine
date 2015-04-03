using Flumine.Data;

namespace Flumine
{
    public class FlumineHost
    {
        private readonly FlumineHostConfig config;
        private readonly IDataStore dataStore;
        private readonly IFlumineWorker worker;

        public FlumineHost(FlumineHostConfig config, IDataStore dataStore, IFlumineWorker worker)
        {
            this.config = config;
            this.dataStore = dataStore;
            this.worker = worker;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }
}
