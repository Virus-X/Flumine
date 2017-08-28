using Flumine.Model;

using Nancy;
using Nancy.ModelBinding;

namespace Flumine.Api
{
    public class ApiModule : NancyModule
    {
        public ApiModule(FlumineHost host)
        {
            Get["/state"] = _ => host.LocalNode;

            Get["/ping"] = _ =>
                new PingResponse
                {
                    Ok = true,
                    Id = host.LocalNode.NodeId
                };

            Put["/shares"] = _ =>
                {
                    var model = this.Bind<ShareAssignmentArgs>();
                    if (model.Shares == null)
                    {
                        return Response.BadRequest("Shares not specified");
                    }

                    host.AssignShares(model);
                    return Response.Success();
                };

            Delete["/shares"] = _ =>
                {
                    var model = this.Bind<ShareAssignmentArgs>();
                    if (model.Shares == null)
                    {
                        return Response.BadRequest("Shares not specified");
                    }

                    host.ReleaseShares(model);
                    return Response.Success();
                };

            Post["/notifications/shutdown"] = _ =>
                {
                    host.NotifyShutdown(this.Bind<NodeDescriptor>());
                    return Response.Success();
                };

            Post["/notifications/startup"] = _ =>
                {
                    host.NotifyStartup(this.Bind<NodeDescriptor>());
                    return Response.Success();
                };
        }
    }
}
