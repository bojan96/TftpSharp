﻿using System;
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
    private readonly int _port;
    private readonly string _filename;
    private readonly TransferMode _transferMode;
    private readonly Stream _stream;
    private readonly int _timeout;
    private readonly int? _blockSize;
    private readonly int _maxTimeoutAttempts;
    private readonly ITransferChannel _transferChannel;
    private readonly IHostResolver _hostResolver;
    private readonly bool _negotiateSize;
    private readonly bool _negotiateTimeout;

    public DownloadSession(string host,
        int port,
        string filename,
        TransferMode transferMode,
        Stream stream,
        int timeout,
        int? blockSize,
        int maxTimeoutAttempts,
        ITransferChannel transferChannel,
        IHostResolver hostResolver,
        bool negotiateSize,
        bool negotiateTimeout)
    {
        _host = host;
        _port = port;
        _filename = filename;
        _transferMode = transferMode;
        _stream = stream;
        _timeout = timeout;
        _blockSize = blockSize;
        _maxTimeoutAttempts = maxTimeoutAttempts;
        _transferChannel = transferChannel;
        _hostResolver = hostResolver;
        _negotiateSize = negotiateSize;
        _negotiateTimeout = negotiateTimeout;
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        var sessionHostIp = await _hostResolver.ResolveHostToIpv4AddressAsync(_host, cancellationToken);
        var context = new TftpContext(_transferChannel, _stream, _filename, _transferMode, _port, sessionHostIp)
        {
            Timeout = _timeout,
            MaxTimeoutAttempts = _maxTimeoutAttempts
        };
        if (_blockSize is not null)
            context.Options.Add("blksize", _blockSize.ToString()!);

        if(_negotiateSize)
            context.Options.Add("tsize", "0");

        if(_negotiateTimeout)
            context.Options.Add("timeout", _timeout.ToString());

        var stateMachineRunner = new StateMachineRunner();
        await stateMachineRunner.RunAsync(new SendRrqState(1), context,
             cancellationToken);
    }
}