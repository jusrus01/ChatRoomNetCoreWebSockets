using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using Server.Models;
using System.Threading;

namespace Server
{
    public class ConnectionsManager
    {
        public ConcurrentDictionary<User, WebSocket> Sockets { private set; get; }
        
        public ConnectionsManager()
        {
            Sockets = new ConcurrentDictionary<User, WebSocket>();
        }

        public string AddSocket(string username, WebSocket socket)
        {
            string id = Guid.NewGuid().ToString();

            if(Sockets.TryAdd(new User { Username = username, Id = id} , socket))
            {
                Console.WriteLine($"ConnectionsManager->AddSocket: WebSocket added with id {id}");
                return id;
            }

            throw new Exception("ConnectionsManager->AddSocket: Failed to add a new WebSocket");
        }

        public async void CloseAllConnections()
        {
            foreach(var sock in Sockets)
            {
                await sock.Value.CloseAsync(WebSocketCloseStatus.EndpointUnavailable,
                    null, CancellationToken.None);
            }
        }
    }
}