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
        private const int MinBlockSize = 8;
        private const int MaxBlockSize = 65464;

        private readonly UdpClient _udpClient = new();
        private int? _blockSize;
        
        public string Host { get; }
        public int Timeout { get; set; } = 3000;

        public int? BlockSize
        {
            get => _blockSize;
            set
            {
                if (value is not null && (value < MinBlockSize || value > MaxBlockSize))
                    throw new ArgumentException($"Must be between {MinBlockSize} and {MaxBlockSize}",
                        nameof(BlockSize));

                _blockSize = value;
            }
        }

        public TftpClient(string host)
        {
            Host = host;
        }

        public async Task DownloadStreamAsync(string remoteFilename, Stream stream,
            CancellationToken cancellationToken = default)
            => await new DownloadSession(_udpClient, Host, remoteFilename, TransferMode.Octet, stream, Timeout, _blockSize).Start(
                cancellationToken);

        public async Task UploadStreamAsync(string remoteFilename, Stream stream,
            CancellationToken cancellationToken = default)
            => await new UploadSession(_udpClient, Host, remoteFilename, TransferMode.Octet, stream, Timeout, _blockSize).Start(
                cancellationToken);

        public void Dispose()
        {
            _udpClient.Dispose();
        }
    }
}
