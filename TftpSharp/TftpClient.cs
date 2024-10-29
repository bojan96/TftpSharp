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
using TftpSharp.Dns;
using TftpSharp.TransferChannel;

namespace TftpSharp
{
    public class TftpClient
    {
        private const int MinBlockSize = 8;
        private const int MaxBlockSize = 65464;
        private int? _blockSize;
        private int _maxTimeoutAttempts = 5;
        private int _timeout = 3;

        public string Host { get; }

        public int Timeout
        {
            get => _timeout;
            set
            {
                if (value < 1 || value > 255)
                    throw new ArgumentOutOfRangeException(nameof(Timeout), "Must be between 1 and 255");

                _timeout = value;
            }
        }

        public TransferMode TransferMode { get; set; } = TransferMode.Octet;
        public bool NegotiateSize { get; set; } = false;
        public int MaxTimeoutAttempts
        {
            get => _maxTimeoutAttempts;
            set => _maxTimeoutAttempts = value < 1 ? throw new ArgumentException("Must be greater than or equal to 1", nameof(MaxTimeoutAttempts)) : value;
        }

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
        {
            var hostResolver = new DnsHostResolver();
            using var transferChannel = new UdpTransferChannel();
            var session =
                new DownloadSession(Host, remoteFilename, TransferMode, stream, Timeout, _blockSize, _maxTimeoutAttempts, transferChannel, hostResolver, NegotiateSize);
            await session.Start(
                cancellationToken);
        }

        public async Task UploadStreamAsync(string remoteFilename, Stream stream,
            CancellationToken cancellationToken = default)
        {
            var hostResolver = new DnsHostResolver();
            using var transferChannel = new UdpTransferChannel();
            var session =
                new UploadSession(Host, remoteFilename, TransferMode, stream, Timeout, _blockSize, _maxTimeoutAttempts, transferChannel, hostResolver, NegotiateSize);
            await session.Start(cancellationToken);
        }
        
    }
}
