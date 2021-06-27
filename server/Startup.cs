using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Server.Middlewares;

namespace Server
{
    public class Startup
    {
        private ConnectionsManager _manager;
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebSocketServerConnectionManager();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var hostAppLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            hostAppLifetime.ApplicationStopping.Register(OnShutDown);

            _manager = app.ApplicationServices.GetRequiredService<ConnectionsManager>();

            app.UseWebSockets();
            app.UseWebSocketServer();

            app.Run(async context =>
            {
                await context.Response.WriteAsync("Hello world");
            });
        }

        public void OnShutDown()
        {
            if(_manager != null)
            {
                _manager.CloseAllConnections();
            }            
        }
    }
}
