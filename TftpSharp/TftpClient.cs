using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TftpSharp
{
    public class TftpClient
    {
        
        public string Host { get; }

        public TftpClient(string host)
        {
            Host = host;
        }


        public async Task UploadStream(string filename, Stream stream)
        {

        }

        public async Task ReadStream(string remoteFilename, Stream stream)
        {
            using var client = new UdpClient();

            var packet = CreateRRQPacket(remoteFilename);
            var bytes = await client.SendAsync(packet, packet.Length, Host, 69);
            var recvTask = client.ReceiveAsync();
            var delayTask = Task.Delay(2000);
            var resultTask = await Task.WhenAny(recvTask, delayTask);
            if (resultTask == delayTask)
            {
                Console.WriteLine("Timeout");
                await client.ReceiveAsync();
                return;
            }

            var receiveResult = await recvTask;
            var ackPacket = CreateAckPacket(1);
            await client.SendAsync(ackPacket, ackPacket.Length, Host, receiveResult.RemoteEndPoint.Port);

            var lastRecvPacket = receiveResult.Buffer.Skip(4).ToArray();
            ushort packetToAck = 2;
            while (lastRecvPacket.Length == 512)
            {
                var recvResult = await client.ReceiveAsync();
                lastRecvPacket = recvResult.Buffer.Skip(4).ToArray();
                ackPacket = CreateAckPacket(packetToAck++);
                await client.SendAsync(ackPacket, ackPacket.Length, Host, receiveResult.RemoteEndPoint.Port);
            }

        }

        private byte[] CreateRRQPacket(string filename)
        {
            var filenamePart = Encoding.UTF8.GetBytes(filename);
            var octet = Encoding.UTF8.GetBytes("octet");
            var packet = new byte[]{ 0 , 1 }
                .Concat(filenamePart)
                .Concat(new byte[] { 0 })
                .Concat(octet)
                .Concat(new byte[] { 0 }).ToArray();

            return packet;
        }

        private byte[] CreateAckPacket(ushort blockNumber)
        {
            var blockBytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(blockBytes, blockNumber);
            return new byte[] { 0, 4 }.Concat(blockBytes).ToArray();
        }
    }
}
