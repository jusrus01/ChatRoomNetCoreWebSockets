using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Server.Models;

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
                Console.WriteLine($"WebSocketServerMiddleware->InvokeAsync: Waiting for username...");
                
                // wait for client to send username
                string username = await ReceiveUsername(webSocket);

                if(username == null)
                {
                    Console.WriteLine($"WebSocketServerMiddleware->InvokeAsync: Username was not set, closing!");
                    await webSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, null, CancellationToken.None);
                    return;
                }

                Console.WriteLine($"WebSocketServerMiddleware->InvokeAsync: Trying to add user to session");

                string id;
                try
                {
                    id =  _manager.AddSocket(username, webSocket);
                }
                catch
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, null, CancellationToken.None);
                    return;
                }

                Console.WriteLine($"WebSocketServerMiddleware->InvokeAsync: User was added!");

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
                        User user = _manager.Sockets.FirstOrDefault(soc => soc.Value == webSocket).Key;

                        WebSocket socket;
                        _manager.Sockets.TryRemove(user, out socket);

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

        // need to handle if such username already exists
        private async Task<string> ReceiveUsername(WebSocket socket)
        {
            byte[] buffer = new byte[35];
            await socket.ReceiveAsync(buffer, CancellationToken.None);

            string json = Encoding.UTF8.GetString(buffer);
            Console.WriteLine(json);
            string username;

            try
            {
                var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
                username = jsonObject.Username;
            }
            catch(JsonReaderException)
            {
                return null;
            }

            return username;
        }
        
        private async Task RouteJSONMessageAsync(string msg) // might need to add JSON validation
        {
            dynamic routeOb;
            try
            {
                routeOb = JsonConvert.DeserializeObject<dynamic>(msg);
            }
            catch
            {
                return;
            }
            Guid guidOutput;

            if(Guid.TryParse(routeOb.To.ToString(), out guidOutput))
            {
                string toGuid = routeOb.To;
                var recipientSocket = _manager.Sockets.FirstOrDefault(soc => soc.Key.Id == toGuid);

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
                string json = JsonConvert.SerializeObject(new { Message = routeOb.Message.ToString(), Username = routeOb.Username.ToString() }, Formatting.Indented);
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