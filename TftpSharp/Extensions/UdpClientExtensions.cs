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
            await client.SendAsync(packetBytes, endpoint, cancellationToken);
        }

        public static async Task<UdpReceiveResult> ReceiveFromAddressAsync(this UdpClient client, IPAddress address, CancellationToken cancellationToken = default)
        {
            UdpReceiveResult result;
            do
            {
                result = await client.ReceiveAndIgnoreConnectResetAsync(cancellationToken);
            } while (!result.RemoteEndPoint.Address.Equals(address));

            return result;
        }

        public static async Task<UdpReceiveResult> ReceiveFromAsync(this UdpClient client, IPEndPoint endpoint, CancellationToken cancellationToken = default)
        {
            UdpReceiveResult result;
            do
            {
                result = await client.ReceiveAndIgnoreConnectResetAsync(cancellationToken);
            } while (!result.RemoteEndPoint.Equals(endpoint));

            return result;
        }

        private static async Task<UdpReceiveResult> ReceiveAndIgnoreConnectResetAsync(this UdpClient client, CancellationToken cancellationToken = default)
        {
            var result = new UdpReceiveResult();
            bool retry;
            do
            {
                try
                {
                    result = await client.ReceiveAsync(cancellationToken);
                    retry = false;
                }
                // On Windows if udp send fails then ICMP Port Unreachable is received.
                // This causes ReceiveAsync to throw ConnectionReset exception (even though UDP has no concept of "connection")
                catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
                {
                    retry = true;
                }
            } while (retry);

            return result;
        }
    }
}
