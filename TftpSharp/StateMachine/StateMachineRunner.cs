using System.Threading;
using System.Threading.Tasks;

namespace TftpSharp.StateMachine
{
    internal class StateMachineRunner
    {
        public async Task RunAsync<TContext>(IState<TContext> initialState, TContext context, CancellationToken cancellationToken = default)
        {
            var currentState = initialState;

            while (currentState is not EndState<TftpContext>)
                currentState = await currentState.HandleAsync(context, cancellationToken);
        }
    }
}
