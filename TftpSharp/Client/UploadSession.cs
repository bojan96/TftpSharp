using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Dns;
using TftpSharp.Exceptions;
using TftpSharp.Extensions;
using TftpSharp.Packet;
using TftpSharp.StateMachine;
using TftpSharp.TransferChannel;

namespace TftpSharp.Client;

internal class UploadSession
{
    private readonly string _host;
    private readonly string _filename;
    private readonly TransferMode _transferMode;
    private readonly Stream _stream;
    private readonly TimeSpan _timeout;
    private readonly int? _blockSize;
    private readonly int _maxTimeoutAttempts;
    public readonly ITransferChannel _transferChannel;
    private readonly IHostResolver _hostResolver;
    private readonly bool _negotiateSize;

    public UploadSession(string host, string filename, TransferMode transferMode, Stream stream, TimeSpan timeout,
        int? blockSize, int maxTimeoutAttempts, ITransferChannel transferChannel, IHostResolver hostResolver,
        bool negotiateSize)
    {
        _host = host;
        _filename = filename;
        _transferMode = transferMode;
        _stream = stream;
        _timeout = timeout;
        _blockSize = blockSize;
        _maxTimeoutAttempts = maxTimeoutAttempts;
        _transferChannel = transferChannel;
        _hostResolver = hostResolver;
        _negotiateSize = negotiateSize;
    }


    public async Task Start(CancellationToken cancellationToken = default)
    {
        var sessionHostIp = await _hostResolver.ResolveHostToIpv4AddressAsync(_host, cancellationToken);
        var context = new TftpContext(_transferChannel, _stream, _filename, _transferMode, 69, sessionHostIp)
        {
            Timeout = _timeout,
            MaxTimeoutAttempts = _maxTimeoutAttempts,
            NegotiateSize = _negotiateSize
        };

        if(_blockSize is not null)
            context.Options.Add("blksize", _blockSize.ToString()!);

        try
        {
            if(_negotiateSize)
                context.Options.Add("tsize", _stream.Length.ToString());
        }
        catch (NotSupportedException)
        {
        }

        var stateMachineRunner = new StateMachineRunner();
        await stateMachineRunner.RunAsync(new SendWrqState(1), context,
             cancellationToken);
    }
}