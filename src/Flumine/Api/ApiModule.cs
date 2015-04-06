
using Flumine.Nancy.Model;

using Nancy;
using Nancy.ModelBinding;

namespace Flumine.Nancy
{
    public class ApiModule : NancyModule
    {
        public ApiModule(FlumineHost host)
        {
            Get["/state"] = _ => new NodeStateModel(host.GetState());

            Get["/ping"] = _ => true;

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
                    var model = this.Bind<NodeStateModel>();
                    host.NotifyShutdown(model.ToNodeDescriptor());
                    return Response.Success();
                };

            Post["/notifications/startup"] = _ =>
                {
                    var model = this.Bind<NodeStateModel>();
                    host.NotifyStartup(model.ToNodeDescriptor());
                    return Response.Success();
                };
        }
    }
}
