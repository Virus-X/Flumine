using System;

namespace Flumine.Nancy.Model
{
    public class ShareAssignmentModel
    {
        public Guid NodeId { get; set; }

        public int[] Shares { get; set; }
    }
}