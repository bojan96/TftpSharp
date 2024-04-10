using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Exceptions;

namespace TftpSharp.Dns
{
    internal class DnsHostResolver : IHostResolver
    {
        public async Task<IPAddress> ResolveHostToIpv4AddressAsync(string host, CancellationToken cancellationToken = default)
        {
            var ipAddresses = await System.Net.Dns.GetHostAddressesAsync(host, cancellationToken);
            if (ipAddresses.Length == 0)
                throw new TftpException($"{host}:No such host is known");

            var sessionHostIp = ipAddresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)!;
            return sessionHostIp;
        }
    }
}
