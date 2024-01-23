using System.Threading;
using System.Threading.Tasks;

namespace TftpSharp.StateMachine
{
    internal interface IState<TContext>
    {

        Task<IState<TContext>> HandleAsync(TContext context, CancellationToken cancellationToken = default);
    }
}
