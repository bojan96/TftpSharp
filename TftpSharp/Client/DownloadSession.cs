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
            }, 3000, 5,cancellationToken);

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
                    }, 3000, 5, cancellationToken);

                if (recvPacket is ErrorPacket errorPacket)
                    throw new TftpErrorResponseException(errorPacket.Code, errorPacket.ErrorMessage);

                var recvDataPacket = (DataPacket)recvPacket;
                await _stream.WriteAsync(recvDataPacket.Data, cancellationToken);
                lastRecvDataPacket = recvDataPacket;
            }

            await AckAndDally(lastRecvDataPacket.BlockNumber, remoteEndpoint, cancellationToken);

        }

        private async Task<(Packet.Packet receivedPacket, IPEndPoint remoteEndpoint)> SendAndReceiveWithRetry(
            Packet.Packet packet, IPEndPoint endpoint, Func<CancellationToken, Task<(Packet.Packet packet, IPEndPoint endpoint)>> receiveOperation, 
            int timeout, int transmitAttempts, CancellationToken cancellationToken = default)
        {
            bool retry;
            IPEndPoint? recvEndpoint = null;
            Packet.Packet? receivedPacket = null;
            var attempts = transmitAttempts;


            do
            {
                try
                {
                    await _udpClient.SendTftpPacketAsync(packet, endpoint, cancellationToken);
                    (receivedPacket, recvEndpoint) = await WithTimeout(receiveOperation, timeout, cancellationToken);
                    retry = false;
                }
                catch (ReceiveTimeoutException)
                {
                    retry = true;
                }

            } while (retry && --attempts > 0);

            if(attempts == 0)
                throw new TftpTimeoutException(transmitAttempts);

            return (receivedPacket!, recvEndpoint!);
        }

        public static async Task<TResult> WithTimeout<TResult>(Func<CancellationToken, Task<TResult>> operation, int timeout, CancellationToken cancellationToken = default)
        {
            using var rcvTaskCancellationSource = new CancellationTokenSource();
            await using var cancellationTokenRegistration = cancellationToken.Register(() => rcvTaskCancellationSource.Cancel());
            var receiveTask = operation(rcvTaskCancellationSource.Token);
            var timeoutTask = Task.Delay(timeout, cancellationToken);

            var resultTask = await Task.WhenAny(receiveTask, timeoutTask);
            if (resultTask == receiveTask)
                return await receiveTask;

            rcvTaskCancellationSource.Cancel();
            throw new ReceiveTimeoutException();
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
                            }, 3000,
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
