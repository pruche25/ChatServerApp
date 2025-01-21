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
        private const int ListenQueueSize = 100; // 리스닝 큐 크기
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
            listenSocket.Listen(ListenQueueSize);
            RegisterAccept(null);
        }

        private void RegisterAccept(SocketAsyncEventArgs args)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"RegisterAccept Error: {ex.Message}");
            }
        }

        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            try
            {
                if (args.SocketError == SocketError.Success)
                {
                    Guid clientId = Guid.NewGuid();
                    Console.WriteLine($"Client Connected: {clientId}");
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnAcceptCompleted Error: {ex.Message}");
                if(args.AcceptSocket != null)
                {
                    args.AcceptSocket.Close();
                }
            }
            finally
            {
                RegisterAccept(args);
            }
        }

        public void BroadcastMessage(string message, Guid guid)
        {
            foreach (var client in clients)
            {
                if (client.Key != guid)
                {
                    try
                    {
                        client.Value.SendMessage(message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Broadcasting Error: {ex.Message}");
                    }
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
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Stop Error: {ex.Message}");
            }
        }
    }
}
