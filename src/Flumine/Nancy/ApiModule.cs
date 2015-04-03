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

            Put["/shares"] = _ =>
                {
                    var model = this.Bind<ShareAssignmentModel>();
                    if (model.Shares == null)
                    {
                        return Response.BadRequest("Shares not specified");
                    }

                    host.AssignShares(model.Shares);
                    return Response.Success();
                };

            Delete["/shares"] = _ =>
                {
                    var model = this.Bind<ShareAssignmentModel>();
                    if (model.Shares == null)
                    {
                        return Response.BadRequest("Shares not specified");
                    }

                    host.ReleaseShares(model.Shares);
                    return Response.Success();
                };

            Post["/notifications/shutdown"] = _ =>
                {
                    var model = this.Bind<NodeStateModel>();
                    host.ProcessShutdownNotification(model.NodeId, model.AssignedShares);
                    return Response.Success();
                };

            Post["/notifications/startup"] = _ =>
                {
                    var model = this.Bind<NodeStateModel>();
                    host.ProcessStartupNotification(model.NodeId, model.Endpoint);
                    return Response.Success();
                };
        }
    }
}
