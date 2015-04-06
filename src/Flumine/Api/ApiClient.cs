using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using Flumine.Api;
using Flumine.Model;
using Flumine.Nancy.Model;

using RestSharp;

namespace Flumine.Nancy
{
    public class ApiClient : IMasterApi, INodeApi
    {
        private readonly IRestClient client;

        public ApiClient(string endpoint)
        {
            client = new RestClient(endpoint);
        }

        public NodeDescriptor GetState()
        {
            var res = Execute<NodeStateModel>(new RestRequest("state", Method.GET));
            return new NodeDescriptor
            {
                Endpoint = res.Endpoint,
                AssignedShares = new List<int>(res.AssignedShares ?? Enumerable.Empty<int>()),
                NodeId = res.NodeId
            };
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
            Execute(new RestRequest("notifications/startup", Method.POST), new NodeStateModel(node));
        }

        public void NotifyShutdown(NodeDescriptor node)
        {
            Execute(new RestRequest("notifications/shutdown", Method.POST), new NodeStateModel(node));
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
