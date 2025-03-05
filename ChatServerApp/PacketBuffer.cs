using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServerApp
{
    internal class PacketBuffer
    {
        private readonly int HeaderSize;
        private readonly int InitialBufferSize; // 초기 버퍼 크기 상수
        private readonly List<byte> buffer;

        public PacketBuffer()
        {
            HeaderSize = int.Parse(ConfigurationManager.AppSettings.Get("headerSize"));
            InitialBufferSize = int.Parse(ConfigurationManager.AppSettings.Get("bufferSize"));
            buffer = new List<byte>(InitialBufferSize);
        }

        public void AddData(byte[] data)
        {
            buffer.AddRange(data);
        }
        public bool GetPacket(out byte[] packet)
        {
            packet = null;
            if (buffer.Count < HeaderSize)
            {
                return false;
            }
            int packetSize = BitConverter.ToInt32(buffer.ToArray(), 0);

            // 유효하지 않은 패킷 크기 처리
            if (packetSize <= 0)
            { 
                //Console.WriteLine("Invalid packet size: " + packetSize);
                // 버퍼 초기화 또는 유효하지 않은 데이터 제거
                buffer.Clear();
                return false;
            }

            if (buffer.Count < packetSize + HeaderSize)
            {
                return false;
            }

            packet = buffer.GetRange(HeaderSize, packetSize).ToArray();
            buffer.RemoveRange(0, HeaderSize + packetSize);
            return true;
        }

        public byte[] CreatePacket(string message)
        {
            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
            int packetSize = messageBytes.Length;

            byte[] packetSizeBytes = BitConverter.GetBytes(packetSize);
            byte[] packet = new byte[HeaderSize + packetSize];

            Array.Copy(packetSizeBytes, 0, packet, 0, HeaderSize);
            Array.Copy(messageBytes, 0, packet, HeaderSize, packetSize);

            return packet;
        }

        public void Clear()
        {
            buffer.Clear();
        }
    }
}
