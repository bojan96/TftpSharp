using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Exceptions;
using TftpSharp.Packet;

namespace TftpSharp.StateMachine;

internal class ErrorPacketReceivedState : IState<TftpContext>
{
    private readonly ErrorPacket _errorPacket;

    public ErrorPacketReceivedState(ErrorPacket errorPacket)
    {
        _errorPacket = errorPacket;
    }

    public Task<IState<TftpContext>> HandleAsync(TftpContext context, CancellationToken cancellationToken = default)
        => throw new TftpErrorResponseException(_errorPacket.Code, _errorPacket.ErrorMessage);
}