using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Exceptions;
using TftpSharp.Extensions;
using TftpSharp.Packet;

namespace TftpSharp.StateMachine
{
    internal abstract class InitialReceiveState : TimeoutState
    {
        public sealed override async Task<IState<TftpContext>> HandleStateAsync(TftpContext context, CancellationToken cancellationToken = default)
        {
            IState<TftpContext>? state = null;
            bool retry;
            do
            {
                try
                {
                    var result = await context.Client.ReceiveFromAddressAsync(context.Host, cancellationToken);
                    context.TransferId = result.RemoteEndPoint.Port;
                    var packet = PacketParser.Parse(result.Buffer);
                    state = await HandleReceiveStateAsync(packet, context, cancellationToken);
                    retry = state is null;
                }
                catch (TftpInvalidPacketException)
                {
                    retry = true;
                }
            } while (retry);

            return state!;
        }

        protected abstract Task<IState<TftpContext>?> HandleReceiveStateAsync(Packet.Packet packet, TftpContext context, CancellationToken cancellationToken);
    }
}
