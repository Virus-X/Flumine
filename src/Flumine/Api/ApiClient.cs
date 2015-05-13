using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using Flumine.Data;
using Flumine.Model;

using RestSharp;

namespace Flumine.Api
{
    public class ApiClient : IMasterApi, INodeApi
    {
        private readonly List<string> endpoints;
        private IRestClient client;

        public ApiClient(INodeDescriptor descriptor)
        {
            endpoints = descriptor.Endpoints ?? new List<string>();
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

        private static string DiscoverConnectableEndpoint(IEnumerable<string> endpoints)
        {
            foreach (var enpoint in endpoints)
            {
                Uri baseUri;
                if (!Uri.TryCreate(enpoint, UriKind.Absolute, out baseUri))
                {
                    continue;
                }

                var uri = new Uri(baseUri, "ping.json");
                var req = WebRequest.Create(uri);
                try
                {
                    using (req.GetResponse())
                    {
                        return enpoint;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private T Execute<T>(IRestRequest request, object body = null)
            where T : new()
        {
            request.RequestFormat = DataFormat.Json;
            if (body != null)
            {
                request.AddBody(body);
            }

            var response = GetClient().Execute<T>(request);
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

            var response = GetClient().Execute(request);
            if (response.ErrorException != null)
            {
                throw response.ErrorException;
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidDataException("Received response " + response.StatusCode);
            }
        }

        private IRestClient GetClient()
        {
            if (client != null)
            {
                return client;
            }

            var endpoint = DiscoverConnectableEndpoint(endpoints);
            if (string.IsNullOrEmpty(endpoint))
            {
                throw new IOException("Cannot establish connection. All endpoints are unreachable.");
            }

            client = new RestClient(endpoint);
            return client;
        }
    }
}
