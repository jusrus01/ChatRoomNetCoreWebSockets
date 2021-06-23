using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Server.Middlewares
{
    public class WebSocketServerMiddleware
    {
        private readonly RequestDelegate _next;
        private ConnectionsManager _manager;
        public WebSocketServerMiddleware(RequestDelegate next, ConnectionsManager manager)
        {
            _next = next;
            _manager = manager;   
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if(context.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                Console.WriteLine($"WebSocketServerMiddleware->InvokeAsync: Received a connection from {context.Connection.RemoteIpAddress}");

                string id =  _manager.AddSocket(webSocket);

                await SendConnectionId(webSocket, id);
                await Receive(webSocket, async (result, buffer) =>
                {

                });

            }
            else
            {
                Console.WriteLine($"WebSocketServerMiddleware->InvokeAsync: Received not a WebSocket from {context.Connection.RemoteIpAddress}");
                await _next(context);
            }
        }

        private async Task SendConnectionId(WebSocket socket, string id)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(id);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            byte[] buffer = new byte[1024];

            while(socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                handleMessage(result, buffer);
            }

        }
    }
}