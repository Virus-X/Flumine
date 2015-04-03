using Nancy;

namespace Flumine.Api
{
    public class ApiModule : NancyModule
    {
        public ApiModule()
        {
            Get["/state"] = _ => new NodeStateResponse();
        }
    }
}
