using Flumine.Nancy.Model;

using RestSharp;

namespace Flumine.Nancy
{
    public class ApiClient
    {
        private readonly IRestClient client;

        public ApiClient(string endpoint)
        {
            client = new RestClient(endpoint);
        }

        public NodeStateModel GetState()
        {
            return Execute<NodeStateModel>(new RestRequest("state", Method.GET));
        }

        public void AssignShares(ShareAssignmentModel shareAssignment)
        {
            Execute(new RestRequest("shares", Method.PUT).AddBody(shareAssignment));
        }

        public void ReleaseShares(ShareAssignmentModel shareAssignment)
        {
            Execute(new RestRequest("shares", Method.DELETE).AddBody(shareAssignment));
        }

        public void NotifyStartup(NodeStateModel node)
        {
            Execute(new RestRequest("notifications/startup", Method.POST).AddBody(node));
        }

        public void NotifyShutdown(NodeStateModel node)
        {
            Execute(new RestRequest("notifications/shutdown", Method.POST).AddBody(node));
        }

        private T Execute<T>(IRestRequest request)
            where T : new()
        {
            var response = client.Execute<T>(request);
            if (response.ErrorException != null)
            {
                throw response.ErrorException;
            }

            return response.Data;
        }

        private void Execute(IRestRequest request)
        {
            var response = client.Execute(request);
            if (response.ErrorException != null)
            {
                throw response.ErrorException;
            }
        }
    }
}
