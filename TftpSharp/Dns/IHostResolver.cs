using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TftpSharp.Dns
{
    internal interface IHostResolver
    {
        Task<IPAddress> ResolveHostToIpv4AddressAsync(string host, CancellationToken cancellationToken = default);
    }
}
