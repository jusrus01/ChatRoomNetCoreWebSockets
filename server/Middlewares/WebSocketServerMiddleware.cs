using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

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
                    if(result.MessageType == WebSocketMessageType.Text)
                    {
                        string receivedMsg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await RouteJSONMessageAsync(receivedMsg);
                    }
                    else if(result.MessageType == WebSocketMessageType.Close)
                    {
                        string id = _manager.Sockets.FirstOrDefault(soc => soc.Value == webSocket).Key;

                        WebSocket socket;
                        _manager.Sockets.TryRemove(id, out socket);

                        await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    }
                    return;
                });

            }
            else
            {
                Console.WriteLine($"WebSocketServerMiddleware->InvokeAsync: Received not a WebSocket from {context.Connection.RemoteIpAddress}");
                await _next(context);
            }
        }
        
        private async Task RouteJSONMessageAsync(string msg) // might need to add JSON validation
        {
            var routeOb = JsonConvert.DeserializeObject<dynamic>(msg);
            Guid guidOutput;

            if(Guid.TryParse(routeOb.To.ToString(), out guidOutput))
            {
                string toGuid = routeOb.To;
                var recipientSocket = _manager.Sockets.FirstOrDefault(soc => soc.Key == toGuid);

                if(recipientSocket.Value != null)
                {
                    if(recipientSocket.Value.State == WebSocketState.Open)
                    {
                        string json = JsonConvert.SerializeObject(new { Message = routeOb.Message.ToString() }, Formatting.Indented);
                        await recipientSocket.Value.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text,
                            true, CancellationToken.None);
                    }
                }
            }
            else
            {
                string json = JsonConvert.SerializeObject(new { Message = routeOb.Message.ToString() }, Formatting.Indented);
                byte[] jsonData = Encoding.UTF8.GetBytes(json);

                foreach(var soc in _manager.Sockets)
                {
                    if(soc.Value.State == WebSocketState.Open)
                    {
                        
                        await soc.Value.SendAsync(jsonData, WebSocketMessageType.Text,
                            true, CancellationToken.None);
                    }
                }
            }
        }

        private async Task SendConnectionId(WebSocket socket, string id)
        {   
            string json = JsonConvert.SerializeObject(new { ConnectionId = id }, Formatting.Indented);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            
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