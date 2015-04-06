using System;
using System.Threading;

namespace Flumine.Util
{
    public class SingleEntryTimer : IDisposable
    {
        private readonly TimerCallback callback;

        private readonly int interval;

        private readonly SemaphoreSlim entrySemaphore;

        private readonly Timer timer;

        public SingleEntryTimer(TimerCallback callback, int interval)
        {
            this.callback = callback;
            this.interval = interval;
            timer = new Timer(TimerCallback);
            entrySemaphore = new SemaphoreSlim(1);
        }

        public void Start()
        {
            timer.Change(interval, interval);
        }

        public void Stop()
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void TimerCallback(object state)
        {
            if (!entrySemaphore.Wait(0))
            {
                return;
            }

            try
            {
                callback(state);
            }
            finally
            {
                entrySemaphore.Release();
            }
        }

        public void Dispose()
        {
            entrySemaphore.Dispose();
            timer.Dispose();
        }
    }
}
