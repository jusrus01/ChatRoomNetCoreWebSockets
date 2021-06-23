using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Server
{
    public class ConnectionsManager
    {
        public ConcurrentDictionary<string, WebSocket> Sockets { private set; get; }
        
        public ConnectionsManager()
        {
            Sockets = new ConcurrentDictionary<string, WebSocket>();
        }

        public string AddSocket(WebSocket socket)
        {
            string id = Guid.NewGuid().ToString();

            if(Sockets.TryAdd(id, socket))
            {
                Console.WriteLine($"ConnectionsManager->AddSocket: WebSocket added with id {id}");
                return id;
            }

            throw new Exception("ConnectionsManager->AddSocket: Failed to add a new WebSocket");
        }
    }
}