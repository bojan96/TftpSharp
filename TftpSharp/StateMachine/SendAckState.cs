using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Extensions;
using TftpSharp.Packet;

namespace TftpSharp.StateMachine;

internal class SendAckState : IState<TftpContext>
{
    private readonly ushort _blockNumber;
    private readonly int _attemptCount;

    public SendAckState(ushort blockNumber, int attemptCount)
    {
        _blockNumber = blockNumber;
        _attemptCount = attemptCount;
    }

    public async Task<IState<TftpContext>> HandleAsync(TftpContext context, CancellationToken cancellationToken = default)
    {
        await context.Client.SendTftpPacketAsync(new AckPacket(_blockNumber),
            new IPEndPoint(context.Host, context.TransferId), cancellationToken);

        return new DownloadReceiveState(_blockNumber, _attemptCount);
    }
}