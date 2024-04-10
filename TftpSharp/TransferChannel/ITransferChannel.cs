using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TftpSharp.TransferChannel
{
    internal interface ITransferChannel : IDisposable
    {
        public record ChannelPacket(byte[] Buffer, IPEndPoint RemoteEndPoint);

        Task SendTftpPacketAsync(Packet.Packet packet,
            IPEndPoint endpoint,
            CancellationToken cancellationToken = default);

        Task<ChannelPacket> ReceiveFromAddressAsync(IPAddress address, CancellationToken cancellationToken = default);
    }
}
