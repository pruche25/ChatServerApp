using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatServerApp
{
    internal class ChatServer
    {
        private Socket listenSocket;
        private SocketAsyncEventArgsPool acceptPool;
        private SocketAsyncEventArgsPool ioPool;
        private ConcurrentDictionary<Guid, ClientHandler> clients = new ConcurrentDictionary<Guid, ClientHandler>();

        public ChatServer(int port, int maxConnections)
        {
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            acceptPool = new SocketAsyncEventArgsPool(maxConnections);
            ioPool = new SocketAsyncEventArgsPool(maxConnections * 2);
        }

        public void Start()
        {
            listenSocket.Listen(100);
            RegisterAccept(null);
        }

        private void RegisterAccept(SocketAsyncEventArgs args)
        {
            if (args == null)
            {
                args = acceptPool.Rent();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            }
            else
            {
                args.AcceptSocket = null;
            }

            bool pending = listenSocket.AcceptAsync(args);
            if (!pending)
            {
                OnAcceptCompleted(null, args);
            }
        }

        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            Console.WriteLine($"Accept:{args}");
            if (args.SocketError == SocketError.Success)
            {
                Guid clientId = Guid.NewGuid();
                ClientHandler handler = new ClientHandler(args.AcceptSocket, this, ioPool, clientId);
                if (clients.TryAdd(clientId, handler))
                {
                    handler.StartReceive();
                }
                else
                {
                    Console.WriteLine("Failed to add client to dictionary");
                    args.AcceptSocket.Close();
                }
            }
            RegisterAccept(args);
        }

        public void BroadcastMessage(string message, Guid guid)
        {
            foreach (var client in clients)
            {
                if (client.Key != guid)
                {
                    client.Value.SendMessage(message);
                }
            }
        }

        public void RemoveClient(Guid clientId)
        {
            if (clients.TryRemove(clientId, out ClientHandler handler))
            {
                handler.Close();
            }
        }

        public void Stop()
        {
            if (listenSocket != null)
            {
                listenSocket.Close();
                listenSocket = null;
            }

            foreach (var client in clients)
            {
                client.Value.Close();
            }

            clients.Clear();
            acceptPool.ClearPool();
            ioPool.ClearPool();
        }
    }
}
