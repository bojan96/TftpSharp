using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Exceptions;

namespace TftpSharp.StateMachine
{
    internal class TimeoutedState : IState<TftpContext>
    {
        private readonly int _totalAttempts;

        public TimeoutedState(int totalAttempts)
        {
            _totalAttempts = totalAttempts;
        }

        public Task<IState<TftpContext>> HandleAsync(TftpContext context, CancellationToken cancellationToken = default) 
            => throw new TftpTimeoutException(_totalAttempts);
    }
}
