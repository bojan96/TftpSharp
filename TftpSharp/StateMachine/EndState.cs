using System;
using System.Threading;
using System.Threading.Tasks;

namespace TftpSharp.StateMachine
{
    internal class EndState<TContext> : IState<TContext>
    {
        public Task<IState<TContext>> HandleAsync(TContext context, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
