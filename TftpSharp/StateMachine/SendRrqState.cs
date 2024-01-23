using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Extensions;
using TftpSharp.Packet;

namespace TftpSharp.StateMachine
{
    internal class SendRrqState : IState<TftpContext>
    {
        private readonly int _attemptCount;

        public SendRrqState(int attemptCount)
        {
            _attemptCount = attemptCount;
        }

        public async Task<IState<TftpContext>> HandleAsync(TftpContext context,
            CancellationToken cancellationToken = default)
        {
            await context.Client.SendTftpPacketAsync(new ReadRequestPacket(context.RemoteFilename, context.TransferMode), new IPEndPoint(context.Host, context.Port), cancellationToken);
            return new DownloadInitialReceiveState(_attemptCount);
        }
    }
}
