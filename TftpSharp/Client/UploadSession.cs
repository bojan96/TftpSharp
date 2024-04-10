using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TftpSharp.Exceptions;
using TftpSharp.Extensions;
using TftpSharp.Packet;
using TftpSharp.StateMachine;
using TftpSharp.TransferChannel;

namespace TftpSharp.Client;

internal class UploadSession : Session
{
    private readonly string _host;
    private readonly string _filename;
    private readonly TransferMode _transferMode;
    private readonly Stream _stream;
    private readonly TimeSpan _timeout;
    private readonly int? _blockSize;
    public readonly ITransferChannel _transferChannel;

    public UploadSession(string host, string filename, TransferMode transferMode, Stream stream, TimeSpan timeout, int? blockSize, ITransferChannel transferChannel)
    {
        _host = host;
        _filename = filename;
        _transferMode = transferMode;
        _stream = stream;
        _timeout = timeout;
        _blockSize = blockSize;
        _transferChannel = transferChannel;
    }


    public async Task Start(CancellationToken cancellationToken = default)
    {
        var sessionHostIp = await ResolveHostAsync(_host, cancellationToken);
        var context = new TftpContext(_transferChannel, _stream, _filename, _transferMode, 69, sessionHostIp)
        {
            Timeout = _timeout
        };
        if(_blockSize is not null)
            context.Options.Add("blksize", _blockSize.ToString()!);

        var stateMachineRunner = new StateMachineRunner();
        await stateMachineRunner.RunAsync(new SendWrqState(1), context,
             cancellationToken);

    }

}