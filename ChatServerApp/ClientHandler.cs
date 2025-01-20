using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServerApp
{
    internal class ClientHandler
    {
        private Socket clientSocket;
        private ChatServer server;
        private SocketAsyncEventArgsPool ioPool;
        private Guid clientId;

        private readonly PacketBuffer packetBuffer;

        public ClientHandler(Socket clientSocket, ChatServer server, SocketAsyncEventArgsPool ioPool, Guid clientId)
        {
            this.clientSocket = clientSocket;
            this.server = server;
            this.ioPool = ioPool;
            this.clientId = clientId;

            packetBuffer = new PacketBuffer();
        }

        public void StartReceive()
        {
            SocketAsyncEventArgs receiveEventArg = ioPool.Rent();
            receiveEventArg.SetBuffer(new byte[1024], 0, 1024);
            receiveEventArg.Completed += OnReceiveComplected;
            receiveEventArg.UserToken = this;
            bool pending = clientSocket.ReceiveAsync(receiveEventArg);
            if (!pending)
            {
                ProcessReceive(receiveEventArg);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    packetBuffer.AddData(args.Buffer);

                    ProcessPackets();
                    StartReceive();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ProcessReceive Error: {ex.Message}");
                }
            }
            else
            {
                Close();
            }
        }

        private void ProcessPackets()
        {
            while (packetBuffer.GetPacket(out var packet))
            {
                string message = Encoding.UTF8.GetString(packet);
                server.BroadcastMessage(message, clientId);
            }
        }

        private void OnReceiveComplected(object sender, SocketAsyncEventArgs args)
        {
            switch (args.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(args);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(args);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }

        }

        private void ProcessSend(SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                Close();
            }
            args.Completed -= OnReceiveComplected;
            ioPool.Return(args);
        }

        public void SendMessage(string message)
        {
            SocketAsyncEventArgs sendEventArg = ioPool.Rent();
            byte[] messageBuffer = packetBuffer.CreatePacket(message);

            try
            {
                sendEventArg.SetBuffer(messageBuffer, 0, messageBuffer.Length);
                sendEventArg.Completed += OnReceiveComplected;
                sendEventArg.UserToken = this;

                bool pending = clientSocket.SendAsync(sendEventArg);
                if (!pending)
                {
                    ProcessSend(sendEventArg);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketException occurred: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
            }
        }

        public void Close()
        {
            if (clientSocket == null)
                return;

            try
            {
                if (clientSocket.Connected)
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Close Socket Error: {ex.Message}");
            }
            finally
            {
                server.RemoveClient(clientId);
                clientSocket = null;
            }
        }
    }
}
