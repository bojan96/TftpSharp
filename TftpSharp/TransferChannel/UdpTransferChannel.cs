using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Extensions;

namespace TftpSharp.TransferChannel
{
    internal class UdpTransferChannel : ITransferChannel
    {
        private readonly UdpClient _client = new();

        public async Task SendTftpPacketAsync(Packet.Packet packet, IPEndPoint endpoint,
            CancellationToken cancellationToken = default) 
            => await _client.SendTftpPacketAsync(packet, endpoint, cancellationToken);

        public async Task<ITransferChannel.ChannelPacket> ReceiveFromAddressAsync(IPAddress address, CancellationToken cancellationToken = default)
        {
            var udpResult = await _client.ReceiveFromAddressAsync(address, cancellationToken);

            return new ITransferChannel.ChannelPacket(udpResult.Buffer, udpResult.RemoteEndPoint);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
