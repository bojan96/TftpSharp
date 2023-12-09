﻿using System;
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


            await SendAndReceiveWithRetry(new AckPacket(lastRecvDataPacket.BlockNumber), remoteEndpoint,
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

                            if (packet is not DataPacket)
                                retry = true;
                            else if (packet is DataPacket dataPacket &&
                                     dataPacket.BlockNumber < lastRecvDataPacket.BlockNumber)
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
                }, 3000, 1,cancellationToken);

        }

        private async Task<(Packet.Packet receivedPacket, IPEndPoint remoteEndpoint)> SendAndReceiveWithRetry(
            Packet.Packet packet, IPEndPoint endpoint, Func<CancellationToken, Task<(Packet.Packet packet, IPEndPoint endpoint)>> receiveOperation, 
            int timeout, int transmitAttempts, CancellationToken cancellationToken = default)
        {
            bool retry;
            IPEndPoint? recvEndpoint = null;
            Packet.Packet? receivedPacket = null;


            do
            {
                try
                {
                    await _udpClient.SendTftpPacketAsync(packet, endpoint, cancellationToken);
                    (receivedPacket, recvEndpoint)  = await WithTimeout(receiveOperation, timeout, cancellationToken);
                    if (receivedPacket is ErrorPacket errPacket)
                        throw new TftpErrorResponseException(errPacket.Code, errPacket.ErrorMessage);

                    retry = false;
                }
                catch (ReceiveTimeoutException)
                {
                    retry = true;
                }

            } while (retry && --transmitAttempts > 0);

            return (receivedPacket!, recvEndpoint!);
        }

        public static async Task<TResult> WithTimeout<TResult>(Func<CancellationToken, Task<TResult>> operation, int timeout, CancellationToken cancellationToken = default)
        {
            using var rcvTaskCancellationSource = new CancellationTokenSource();
            cancellationToken.Register(() => rcvTaskCancellationSource.Cancel());
            var receiveTask = operation(rcvTaskCancellationSource.Token);
            var timeoutTask = Task.Delay(timeout, cancellationToken);

            var resultTask = await Task.WhenAny(receiveTask, timeoutTask);
            if (resultTask == receiveTask)
                return await receiveTask;

            rcvTaskCancellationSource.Cancel();
            throw new ReceiveTimeoutException();
        }

    }
}