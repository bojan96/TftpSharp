using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Exceptions;
using TftpSharp.Extensions;
using TftpSharp.Packet;

namespace TftpSharp.Client
{
    internal class DownloadSession : Session
    {
        private readonly string _host;
        private readonly string _filename;
        private readonly TransferMode _transferMode;
        private readonly Stream _stream;
        private readonly int _timeout;

        public DownloadSession(UdpClient udpClient, string host, string filename, TransferMode transferMode, Stream stream, int timeout) : base(udpClient)
        {
            _host = host;
            _filename = filename;
            _transferMode = transferMode;
            _stream = stream;
            _timeout = timeout;
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            var sessionHostIp = await ResolveHostAsync(_host, cancellationToken);

            var (initialPacket, initialRemoteEndpoint) = await SendAndReceiveWithRetry(new ReadRequestPacket(_filename, _transferMode), new IPEndPoint(sessionHostIp, 69), async token =>
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

                        if (packet is not ErrorPacket && packet is not DataPacket)
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
            }, _timeout, 5,cancellationToken);

            if (initialPacket is ErrorPacket errPacket)
                throw new TftpErrorResponseException(errPacket.Code, errPacket.ErrorMessage);

            var lastRecvDataPacket = (DataPacket)initialPacket;
            await _stream.WriteAsync(lastRecvDataPacket.Data, cancellationToken);

            var transferId = initialRemoteEndpoint.Port;
            var remoteEndpoint = new IPEndPoint(sessionHostIp, transferId);

            while (lastRecvDataPacket.Data.Length == 512)
            {

                var (recvPacket, _) = await SendAndReceiveWithRetry(new AckPacket(lastRecvDataPacket.BlockNumber), remoteEndpoint,
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

                                if (packet is not ErrorPacket && packet is not DataPacket)
                                    retry = true;
                                else if (packet is DataPacket dataPacket &&
                                         dataPacket.BlockNumber <= lastRecvDataPacket.BlockNumber)
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
                    }, _timeout, 5, cancellationToken);

                if (recvPacket is ErrorPacket errorPacket)
                    throw new TftpErrorResponseException(errorPacket.Code, errorPacket.ErrorMessage);

                var recvDataPacket = (DataPacket)recvPacket;
                await _stream.WriteAsync(recvDataPacket.Data, cancellationToken);
                lastRecvDataPacket = recvDataPacket;
            }

            await AckAndDally(lastRecvDataPacket.BlockNumber, remoteEndpoint, cancellationToken);

        }

        private async Task AckAndDally(ushort ackBlock, IPEndPoint remoteEndpoint, CancellationToken cancellationToken = default)
        {
            bool resend;

            do
            {
                await _udpClient.SendTftpPacketAsync(new AckPacket(ackBlock), remoteEndpoint,
                    cancellationToken);
                try
                {
                    var finalResult =
                        await WithTimeout(async token =>
                            {
                                bool retry;
                                Packet.Packet? packet = null;

                                do
                                {
                                    try
                                    {
                                        var result = await _udpClient.ReceiveFromAsync(remoteEndpoint, token);
                                        packet = PacketParser.Parse(result.Buffer);
                                        if (packet is not DataPacket)
                                            retry = true;
                                        else if (packet is DataPacket dataPacket &&
                                                 dataPacket.BlockNumber == ackBlock)
                                            retry = false;
                                        else
                                            retry = true;

                                    }
                                    catch (TftpInvalidPacketException)
                                    {
                                        retry = true;
                                    }

                                } while (retry);

                                return packet;
                            }, _timeout,
                            cancellationToken);

                    resend = true;
                }
                catch (ReceiveTimeoutException e)
                {
                    resend = false;
                }

            } while (resend);

        }
    }
}
