using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Client;

namespace TftpSharp
{
    public class TftpClient : IDisposable
    {
        private readonly UdpClient _udpClient = new();

        public string Host { get; }

        public TftpClient(string host)
        {
            Host = host;
        }

        public async Task DownloadStreamAsync(string remoteFilename, Stream stream,
            CancellationToken cancellationToken = default)
            => await new DownloadSession(_udpClient, Host, remoteFilename, TransferMode.Octet, stream).Start(
                cancellationToken);

        public void Dispose()
        {
            _udpClient.Dispose();
        }
    }
}
