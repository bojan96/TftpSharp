using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Exceptions;
using TftpSharp.Extensions;
using TftpSharp.Packet;

namespace TftpSharp.Client
{
    internal class DownloadSession
    {
        private readonly UdpClient _udpClient;
        private readonly string _host;
        private readonly string _filename;
        private readonly TransferMode _transferMode;
        private readonly Stream _stream;

        public DownloadSession(UdpClient udpClient, string host, string filename, TransferMode transferMode, Stream stream)
        {
            _udpClient = udpClient;
            _host = host;
            _filename = filename;
            _transferMode = transferMode;
            _stream = stream;
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            var ipAddresses = await Dns.GetHostAddressesAsync(_host, cancellationToken);
            if (ipAddresses.Length == 0)
                throw new TftpException($"{_host}:No such host is known");

            var sessionHostIp = ipAddresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)!;
            await _udpClient.SendTftpPacketAsync(new ReadRequestPacket(_filename, _transferMode),
                new IPEndPoint(sessionHostIp, 69), cancellationToken);

            var initialRrqResponse = await _udpClient.ReceiveFromAddressAsync(sessionHostIp, cancellationToken);
            var initialResponsePacket = ParseReceivingPacket(initialRrqResponse.Buffer);

            if (initialResponsePacket is ErrorPacket errPacket)
                throw new TftpErrorResponseException(errPacket.Code, errPacket.ErrorMessage);

            var lastRecvDataPacket = (DataPacket)initialResponsePacket;
            await _stream.WriteAsync(lastRecvDataPacket.Data, cancellationToken);

            var transferId = initialRrqResponse.RemoteEndPoint.Port;
            var remoteEndpoint = new IPEndPoint(sessionHostIp, transferId);
            var lastAckBlock = lastRecvDataPacket.BlockNumber;

            await _udpClient.SendTftpPacketAsync(new AckPacket(lastAckBlock), remoteEndpoint, cancellationToken);

            while (lastRecvDataPacket.Data.Length == 512)
            {
                var udpResult = await _udpClient.ReceiveFromAsync(remoteEndpoint, cancellationToken);
                var packet = ParseReceivingPacket(udpResult.Buffer);
                if (packet is ErrorPacket errorPacket)
                    throw new TftpErrorResponseException(errorPacket.Code, errorPacket.ErrorMessage);

                var recvDataPacket = (DataPacket)packet;
                if(recvDataPacket.BlockNumber <= lastAckBlock)
                    continue;

                await _stream.WriteAsync(recvDataPacket.Data, cancellationToken);
                await _udpClient.SendTftpPacketAsync(new AckPacket(recvDataPacket.BlockNumber), remoteEndpoint, cancellationToken);
                lastAckBlock = recvDataPacket.BlockNumber;
                lastRecvDataPacket = recvDataPacket;
            }

        }

        private Packet.Packet ParseReceivingPacket(byte[] packetBytes)
        {
            var initialRrqPacket = PacketParser.Parse(packetBytes);
            if (initialRrqPacket is not DataPacket && initialRrqPacket is not ErrorPacket)
                throw new TftpInvalidPacketException("Invalid packet received");

            return initialRrqPacket;
        }

    }
}
