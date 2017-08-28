using System;
using Flumine.Util;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

namespace Flumine.Api
{
    public class NancyBootstraper : DefaultNancyBootstrapper
    {
        private static readonly ILog Log = Logger.GetLoggerForDeclaringType();

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

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            pipelines.OnError += (ctx, ex) =>
            {
                var message = ex.Message;

                var aex = ex as AggregateException;
                if (aex != null)
                {
                    message = aex.GetBaseException().Message;
                    Log.Error(aex.GetBaseException());
                }

                return new
                {
                    Message = message,
                    Trace = ex.StackTrace
                };
            };
        }
    }
}