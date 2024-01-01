using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using TftpSharp.Exceptions;
using TftpSharp.Extensions;

namespace TftpSharp.Client
{
    internal abstract class Session
    {

        protected readonly UdpClient _udpClient;

        protected Session(UdpClient udpClient)
        {
            _udpClient = udpClient;
        }

        protected async Task<(Packet.Packet receivedPacket, IPEndPoint remoteEndpoint)> SendAndReceiveWithRetry(
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

            if (attempts == 0)
                throw new TftpTimeoutException(transmitAttempts);

            return (receivedPacket!, recvEndpoint!);
        }

        protected static async Task<TResult> WithTimeout<TResult>(Func<CancellationToken, Task<TResult>> operation, int timeout, CancellationToken cancellationToken = default)
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
    }
}
