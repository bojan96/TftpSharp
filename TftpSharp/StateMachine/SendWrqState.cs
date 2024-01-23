using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Extensions;
using TftpSharp.Packet;

namespace TftpSharp.StateMachine
{
    internal class SendWrqState : IState<TftpContext>
    {
        private readonly int _attemptCount;

        public SendWrqState(int attemptCount)
        {
            _attemptCount = attemptCount;
        }

        public async Task<IState<TftpContext>> HandleAsync(TftpContext context, CancellationToken cancellationToken = default)
        {
            await context.Client.SendTftpPacketAsync(
                new WriteRequestPacket(context.RemoteFilename, context.TransferMode),
                new IPEndPoint(context.Host, context.Port), cancellationToken);

            return new UploadInitialReceiveState(_attemptCount);
        }
    }
}
