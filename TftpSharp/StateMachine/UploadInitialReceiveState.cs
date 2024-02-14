using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Packet;

namespace TftpSharp.StateMachine
{
    internal class UploadInitialReceiveState : InitialReceiveState
    {
        private readonly int _attemptCount;

        public UploadInitialReceiveState(int attemptCount)
        {
            _attemptCount = attemptCount;
        }

        protected override Task<IState<TftpContext>?> HandleReceiveStateAsync(Packet.Packet packet, TftpContext context, CancellationToken cancellationToken)
        {
            return packet switch
            {
                ErrorPacket errPacket => Task.FromResult<IState<TftpContext>?>(new ErrorPacketReceivedState(errPacket)),
                AckPacket { BlockNumber: 0 } => Task.FromResult<IState<TftpContext>?>(new SendDataState(1, 1)),
                OackPacket oackPacket => HandleOackPacket(oackPacket, context),
                _ => Task.FromResult<IState<TftpContext>?>(null)
            };
        }

        private Task<IState<TftpContext>?> HandleOackPacket(OackPacket oackPacket, TftpContext context)
        {
            context.HandleReceivedOptions(oackPacket.Options);
            return Task.FromResult<IState<TftpContext>?>(new SendDataState(1, 1));
        }

        protected override Task<IState<TftpContext>> HandleTimeoutAsync(TftpContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IState<TftpContext>>(_attemptCount == context.MaxTimeoutAttempts
                ? new TimeoutedState(_attemptCount)
                : new SendWrqState(_attemptCount + 1));
    }
}
