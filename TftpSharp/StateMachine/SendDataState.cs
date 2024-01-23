using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Extensions;
using TftpSharp.Packet;

namespace TftpSharp.StateMachine;

internal class SendDataState : IState<TftpContext>
{
    private readonly int _attemptCount;
    private readonly ushort _blockNumber;

    public SendDataState(int attemptCount, ushort blockNumber)
    {
        _attemptCount = attemptCount;
        _blockNumber = blockNumber;
    }

    public async Task<IState<TftpContext>> HandleAsync(TftpContext context, CancellationToken cancellationToken = default)
    {
        if (context.LastReadBlock is null)
        {
            // TODO: Remove hardcoded block size
            var block = new byte[512];
            var bytesRead = await context.Stream.ReadAsync(block, cancellationToken);
            context.LastReadBlock = block[..bytesRead];
        }

        await context.Client.SendTftpPacketAsync(new DataPacket(_blockNumber, context.LastReadBlock),
            new IPEndPoint(context.Host, context.TransferId), cancellationToken);

        return new UploadReceiveState(_attemptCount, _blockNumber);
    }
}