using Nancy;
using Nancy.TinyIoc;

namespace Flumine.Nancy
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
            base.ConfigureApplicationContainer(container);
            container.Register(host);
        }
    }
}