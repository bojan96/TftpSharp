using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Packet;

namespace TftpSharp.StateMachine
{
    internal class UploadReceiveState : ReceiveState
    {
        private readonly int _attemptCount;
        private readonly ushort _lastSentBlockNumber;

        public UploadReceiveState(int attemptCount, ushort lastSentBlockNumber)
        {
            _attemptCount = attemptCount;
            _lastSentBlockNumber = lastSentBlockNumber;
        }

        protected override Task<IState<TftpContext>?> HandleReceiveStateAsync(Packet.Packet packet, TftpContext context, CancellationToken cancellationToken)
        {
            switch (packet)
            {
                case ErrorPacket errPacket:
                    return Task.FromResult<IState<TftpContext>?>(new ErrorPacketReceivedState(errPacket));
                case AckPacket ackPacket when ackPacket.BlockNumber == _lastSentBlockNumber:

                    var lastReadBlock = context.LastReadBlock!;
                    context.LastReadBlock = null;

                    if (lastReadBlock.Length < context.BlockSize)
                        return Task.FromResult<IState<TftpContext>?>(new EndState<TftpContext>());

                    // Discard the last read block
                    context.LastReadBlock = null;
                    return Task.FromResult<IState<TftpContext>?>(new SendDataState(1,
                        (ushort)(_lastSentBlockNumber + 1)));
                default:
                    return Task.FromResult<IState<TftpContext>?>(null);
            }
        }

        protected override Task<IState<TftpContext>> HandleTimeoutAsync(TftpContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IState<TftpContext>>(_attemptCount == context.MaxTimeoutAttempts
                ? new TimeoutedState(_attemptCount)
                : new SendDataState(_attemptCount + 1, _lastSentBlockNumber));
    }
}
