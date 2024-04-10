using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Dns;
using TftpSharp.StateMachine;
using TftpSharp.TransferChannel;

namespace TftpSharp.Client;

internal class DownloadSession
{
    private readonly string _host;
    private readonly string _filename;
    private readonly TransferMode _transferMode;
    private readonly Stream _stream;
    private readonly TimeSpan _timeout;
    private readonly int? _blockSize;
    private readonly ITransferChannel _transferChannel;
    private readonly IHostResolver _hostResolver;

    public DownloadSession(string host, string filename, TransferMode transferMode, Stream stream, TimeSpan timeout, int? blockSize, ITransferChannel transferChannel, IHostResolver hostResolver)
    {
        _host = host;
        _filename = filename;
        _transferMode = transferMode;
        _stream = stream;
        _timeout = timeout;
        _blockSize = blockSize;
        _transferChannel = transferChannel;
        _hostResolver = hostResolver;
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        var sessionHostIp = await _hostResolver.ResolveHostToIpv4AddressAsync(_host, cancellationToken);
        var context = new TftpContext(_transferChannel, _stream, _filename, _transferMode, 69, sessionHostIp)
        {
            Timeout = _timeout,
        };
        if (_blockSize is not null)
            context.Options.Add("blksize", _blockSize.ToString()!);

        var stateMachineRunner = new StateMachineRunner();
        await stateMachineRunner.RunAsync(new SendRrqState(1), context,
             cancellationToken);
    }
}