using System;
using System.Security.Cryptography;
using System.Text;

namespace Flumine.Model
{
    /// <summary>
    /// Represents worker node
    /// </summary>
    public class Node
    {
        public string HostName { get; set; }

        public string Endpoint { get; set; }

        public DateTime LastActivity { get; set; }
    }
}
