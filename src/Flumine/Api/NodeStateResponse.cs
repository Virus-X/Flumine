namespace Flumine.Api
{
    public class NodeStateResponse
    {
        public string NodeId { get; set; }

        public int[] AssignedShares { get; set; }

        public string Endpoint { get; set; }
    }
}