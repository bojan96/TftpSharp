using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.StateMachine;

namespace TftpSharp.Client;

internal class DownloadSession : Session
{
    private readonly string _host;
    private readonly string _filename;
    private readonly TransferMode _transferMode;
    private readonly Stream _stream;
    private readonly int _timeout;
    private readonly int? _blockSize;

    public DownloadSession(string host, string filename, TransferMode transferMode, Stream stream, int timeout, int? blockSize)
    {
        _host = host;
        _filename = filename;
        _transferMode = transferMode;
        _stream = stream;
        _timeout = timeout;
        _blockSize = blockSize;
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        var sessionHostIp = await ResolveHostAsync(_host, cancellationToken);
        var context = new TftpContext(_udpClient, _stream, _filename, _transferMode, 69, sessionHostIp)
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