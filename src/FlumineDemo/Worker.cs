using System;
using System.Collections.Generic;
using System.Linq;

using Flumine;

namespace FlumineDemo
{
    public class Worker : IFlumineWorker
    {
        public void AllocateShares(IEnumerable<int> shares)
        {
            Console.WriteLine("Worker: allocating shares [{0}]", string.Join(",", shares.Select(x => x.ToString())));
        }

        public void ReleaseShares(IEnumerable<int> shares)
        {
            Console.WriteLine("Worker: releasing shares [{0}]", string.Join(",", shares.Select(x => x.ToString())));
        }

        public void ReleaseAllShares()
        {
            Console.WriteLine("Worker: releasing all shares");
        }
    }
}