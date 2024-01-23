using System;
using System.Net;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Exceptions;
using TftpSharp.Extensions;
using TftpSharp.Packet;

namespace TftpSharp.StateMachine
{
    internal class DallyState : TimeoutState
    {
        private readonly ushort _blockNumber;

        public DallyState(ushort blockNumber)
        {
            _blockNumber = blockNumber;
        }

        public override async Task<IState<TftpContext>> HandleStateAsync(TftpContext context, CancellationToken cancellationToken = default)
        {
            
            await context.Client.SendTftpPacketAsync(new AckPacket(_blockNumber), new IPEndPoint(context.Host, context.TransferId), cancellationToken: cancellationToken);

            bool retry;
            do
            {
                try
                {
                    var receiveResult = await context.Client.ReceiveFromAddressAsync(context.Host, cancellationToken);
                    var packet = PacketParser.Parse(receiveResult.Buffer);

                    if (packet is DataPacket dataPacket && dataPacket.BlockNumber == _blockNumber)
                        retry = false;
                    else
                        retry = true;
                }
                catch (TftpInvalidPacketException)
                {
                    retry = true;
                }

            } while (retry);
            
            return new DallyState(_blockNumber);
        }

        public override Task<IState<TftpContext>> HandleTimeoutAsync(TftpContext context, CancellationToken cancellationToken = default) 
            => Task.FromResult<IState<TftpContext>>(new EndState<TftpContext>());
    }
}
