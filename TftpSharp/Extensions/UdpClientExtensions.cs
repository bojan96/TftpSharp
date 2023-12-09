using System;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TftpSharp.Extensions
{
    internal static class UdpClientExtensions
    {
        public static async Task SendTftpPacketAsync(this UdpClient client, Packet.Packet packet,
            IPEndPoint endpoint,
            CancellationToken cancellationToken = default)
        {
            var packetBytes = packet.Serialize();
            var bytesSent = 0;
            while (bytesSent < packetBytes.Length)
            {
                bytesSent = await client.SendAsync(packetBytes, endpoint, cancellationToken);
            }
        }

        public static async Task<UdpReceiveResult> ReceiveFromAddressAsync(this UdpClient client, IPAddress address, CancellationToken cancellationToken = default)
        {
            UdpReceiveResult result;
            do
            {
                result = await client.ReceiveAsync(cancellationToken);
            } while (!result.RemoteEndPoint.Address.Equals(address));

            return result;
        }

        public static async Task<UdpReceiveResult> ReceiveFromAsync(this UdpClient client, IPEndPoint endpoint, CancellationToken cancellationToken = default)
        {
            UdpReceiveResult result;
            do
            {
                result = await client.ReceiveAsync(cancellationToken);
            } while (!result.RemoteEndPoint.Equals(endpoint));

            return result;
        }
    }
}
