using System;
using System.Collections.Generic;

namespace Flumine.Model
{
    public class ShareAssignmentArgs
    {
        public Guid MasterNodeId { get; set; }

        public List<int> Shares { get; set; }

        public ShareAssignmentArgs()
        {
        }

        public ShareAssignmentArgs(Guid masterMasterNode, IEnumerable<int> shares)
        {
            MasterNodeId = masterMasterNode;
            Shares = new List<int>(shares);
        }
    }
}