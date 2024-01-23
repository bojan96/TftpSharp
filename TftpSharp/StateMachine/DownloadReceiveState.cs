using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Exceptions;
using TftpSharp.Extensions;
using TftpSharp.Packet;

namespace TftpSharp.StateMachine;

internal class DownloadReceiveState : ReceiveState
{
    private readonly ushort _lastRcvBlockNumber;
    private readonly int _attemptCount;

    public DownloadReceiveState(ushort lastRcvBlockNumber, int attemptCount)
    {
        _lastRcvBlockNumber = lastRcvBlockNumber;
        _attemptCount = attemptCount;
    }

    protected override async Task<IState<TftpContext>?> HandleReceiveStateAsync(Packet.Packet packet, TftpContext context, CancellationToken cancellationToken)
    {
        switch (packet)
        {
            case ErrorPacket errPacket:
                return new ErrorPacketReceivedState(errPacket);
            case DataPacket dataPacket:
                // TODO: Handle invalid block number
                await context.Stream.WriteAsync(dataPacket.Data, cancellationToken);

                // TODO: Remove hardcoded block size
                if (dataPacket.Data.Length == 512)
                    return new SendAckState(dataPacket.BlockNumber, 1);

                return new DallyState(dataPacket.BlockNumber);
            default:
                return null;
        }
    }

    protected override Task<IState<TftpContext>> HandleTimeoutAsync(TftpContext context,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IState<TftpContext>>(_attemptCount == context.MaxTimeoutAttempts
            ? new TimeoutedState(_attemptCount)
            : new SendAckState(_lastRcvBlockNumber, _attemptCount + 1));
}