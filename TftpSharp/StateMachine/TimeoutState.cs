using System.Threading;
using System.Threading.Tasks;

namespace TftpSharp.StateMachine
{
    internal abstract class TimeoutState : IState<TftpContext>
    {
        public async Task<IState<TftpContext>> HandleAsync(TftpContext context, CancellationToken cancellationToken = default)
        {
            using var stateCancellationTokenSource = new CancellationTokenSource();
            await using var cancellationTokenRegistration =
                cancellationToken.Register(() => stateCancellationTokenSource.Cancel());
            var stateTask = HandleStateAsync(context, stateCancellationTokenSource.Token);
            var timeoutTask = Task.Delay(context.Timeout, cancellationToken);
            var resultTask = await Task.WhenAny(stateTask, timeoutTask);

            if (resultTask == stateTask)
                return await stateTask;

            stateCancellationTokenSource.Cancel();
            return await HandleTimeoutAsync(context, cancellationToken);
        }


        public abstract Task<IState<TftpContext>> HandleStateAsync(TftpContext context,
            CancellationToken cancellationToken = default);

        public abstract Task<IState<TftpContext>> HandleTimeoutAsync(TftpContext context,
            CancellationToken cancellationToken = default);
    }
}
