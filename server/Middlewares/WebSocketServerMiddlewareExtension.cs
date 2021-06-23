using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Server.Middlewares
{
    public static class WebSocketServerMiddlewareExtension
    {
        public static IApplicationBuilder UseWebSocketServer(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WebSocketServerMiddleware>();
        }

        public static IServiceCollection AddWebSocketServerConnectionManager(this IServiceCollection services)
        {
            services.AddSingleton<ConnectionsManager>();
            return services;
        }
    }
}