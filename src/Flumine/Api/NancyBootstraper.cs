using Nancy;
using Nancy.TinyIoc;

namespace Flumine.Api
{
    public class NancyBootstraper : DefaultNancyBootstrapper
    {
        private readonly FlumineHost host;

        public NancyBootstraper(FlumineHost host)
        {
            this.host = host;
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            container.Register<ApiModule>();            
            container.Register(host);
        }
    }
}