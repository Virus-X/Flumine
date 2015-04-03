using System.Collections.Generic;

namespace Flumine
{
    /// <summary>
    /// Represents the worker, that uses shares provided by Flumine.
    /// </summary>
    public interface IFlumineWorker
    {
        /// <summary>
        /// Gives shares to worker. 
        /// Worker can process provided shares.
        /// </summary>
        /// <param name="shares">Share ids.</param>
        void AllocateShares(IEnumerable<int> shares);

        /// <summary>
        /// Takes shares back from worker.
        /// Worker should stop processing these shares and cleanup corresponding cache.
        /// </summary>
        /// <param name="shares">Share ids.</param>
        void ReleaseShares(IEnumerable<int> shares);

        /// <summary>
        /// Releases all worker shares.
        /// Worker should stop processing all shares and cleanup all cache.
        /// </summary>
        void ReleaseAllShares();
    }
}