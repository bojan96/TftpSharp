using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Exceptions;
using TftpSharp.Extensions;
using TftpSharp.Packet;

namespace TftpSharp.Client
{
    internal class UploadSession : Session
    {
        private readonly string _host;
        private readonly string _filename;
        private readonly TransferMode _transferMode;
        private readonly Stream _stream;

        public UploadSession(UdpClient udpClient, string host, string filename, TransferMode transferMode, Stream stream) : base(udpClient)
        {
            _host = host;
            _filename = filename;
            _transferMode = transferMode;
            _stream = stream;
        }


        public async Task Start(CancellationToken cancellationToken = default)
        {
            var sessionHostIp = await ResolveHostAsync(_host, cancellationToken);

            var (initialPacket, initialRemoteEndpoint) = await SendAndReceiveWithRetry(new WriteRequestPacket(_filename, _transferMode), new IPEndPoint(sessionHostIp, 69), async token =>
            {
                bool retry;
                Packet.Packet? packet = null;
                var result = new UdpReceiveResult();

                do
                {
                    try
                    {
                        result = await _udpClient.ReceiveFromAddressAsync(sessionHostIp, token);
                        packet = PacketParser.Parse(result.Buffer);

                        if (packet is not ErrorPacket && packet is not AckPacket)
                            retry = true;
                        else
                            retry = false;
                    }
                    catch (TftpInvalidPacketException)
                    {
                        retry = true;
                    }
                } while (retry);


                return (packet!, result.RemoteEndPoint);
            }, 3000, 5, cancellationToken);

            if (initialPacket is ErrorPacket errPacket)
                throw new TftpErrorResponseException(errPacket.Code, errPacket.ErrorMessage);

            //var ackPacket = (AckPacket)initialPacket;
            var transferId = initialRemoteEndpoint.Port;
            var remoteEndpoint = new IPEndPoint(sessionHostIp, transferId);

            ushort blockNumber = 1;
            int bytesRead;
            var block = new byte[512];

            do
            {
                bytesRead = await _stream.ReadAsync(block, cancellationToken);
                var (recvPacket, _) = await SendAndReceiveWithRetry(new DataPacket(blockNumber, block[..bytesRead]), remoteEndpoint,
                    async token =>
                    {
                        bool retry;
                        Packet.Packet? packet = null;
                        var result = new UdpReceiveResult();

                        do
                        {
                            try
                            {
                                result = await _udpClient.ReceiveFromAsync(remoteEndpoint, token);
                                packet = PacketParser.Parse(result.Buffer);

                                if (packet is not ErrorPacket && packet is not AckPacket)
                                    retry = true;
                                else if (packet is AckPacket ackPacket &&
                                         ackPacket.BlockNumber < blockNumber)
                                    retry = true;
                                else
                                    retry = false;
                            }
                            catch (TftpInvalidPacketException)
                            {
                                retry = true;
                            }
                        } while (retry);

                        return (packet!, result.RemoteEndPoint);
                    }, 3000, 5, cancellationToken);

                if (recvPacket is ErrorPacket errorPacket)
                    throw new TftpErrorResponseException(errorPacket.Code, errorPacket.ErrorMessage);

                ++blockNumber;

            } while (bytesRead == block.LongLength);

        }

    }
}
