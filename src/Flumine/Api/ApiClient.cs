using System.IO;
using System.Net;

using Flumine.Data;
using Flumine.Model;

using RestSharp;

namespace Flumine.Api
{
    public class ApiClient : IMasterApi, INodeApi
    {
        private readonly IRestClient client;

        public ApiClient(INodeDescriptor descriptor)            
        {
            client = new RestClient(descriptor.Endpoint);
        }        

        public NodeDescriptor GetState()
        {
            return Execute<NodeDescriptor>(new RestRequest("state", Method.GET));
        }

        public void AssignShares(ShareAssignmentArgs shareAssignment)
        {
            Execute(new RestRequest("shares", Method.PUT), shareAssignment);
        }

        public void ReleaseShares(ShareAssignmentArgs shareAssignment)
        {
            Execute(new RestRequest("shares", Method.DELETE), shareAssignment);
        }

        public void NotifyStartup(NodeDescriptor node)
        {
            Execute(new RestRequest("notifications/startup", Method.POST), node);
        }

        public void NotifyShutdown(NodeDescriptor node)
        {
            Execute(new RestRequest("notifications/shutdown", Method.POST), node);
        }

        public bool IsAlive()
        {
            Execute(new RestRequest("ping", Method.GET));
            return true;
        }

        private T Execute<T>(IRestRequest request, object body = null)
            where T : new()
        {
            request.RequestFormat = DataFormat.Json;
            if (body != null)
            {
                request.AddBody(body);
            }

            var response = client.Execute<T>(request);
            if (response.ErrorException != null)
            {
                throw response.ErrorException;
            }

            return response.Data;
        }

        private void Execute(IRestRequest request, object body = null)
        {
            request.RequestFormat = DataFormat.Json;
            if (body != null)
            {
                request.AddBody(body);
            }

            var response = client.Execute(request);
            if (response.ErrorException != null)
            {
                throw response.ErrorException;
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidDataException("Received response " + response.StatusCode);
            }
        }
    }
}
