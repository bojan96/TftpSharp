using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using TftpSharp.Exceptions;

namespace TftpSharp.Client;

internal abstract class Session
{

    protected readonly UdpClient _udpClient;

    protected Session(UdpClient udpClient)
    {
        _udpClient = udpClient;
    }

    protected static async Task<IPAddress> ResolveHostAsync(string host, CancellationToken cancellationToken = default)
    {
        var ipAddresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        if (ipAddresses.Length == 0)
            throw new TftpException($"{host}:No such host is known");

        var sessionHostIp = ipAddresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)!;

        return sessionHostIp;
    }
}