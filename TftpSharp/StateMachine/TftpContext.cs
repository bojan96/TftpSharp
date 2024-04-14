using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using TftpSharp.TransferChannel;

namespace TftpSharp.StateMachine;

internal class TftpContext
{
    public TftpContext(ITransferChannel channel, Stream stream, string remoteFilename, TransferMode transferMode, int port, IPAddress host)
    {
        Channel = channel;
        Stream = stream;
        RemoteFilename = remoteFilename;
        TransferMode = transferMode;
        Port = port;
        Host = host;
    }

    public ITransferChannel Channel { get; }
    public Stream Stream { get; }
    public string RemoteFilename { get; }
    public TransferMode TransferMode { get; }
    public int Port { get; }
    public int TransferId { get; set; }
    public IPAddress Host { get; }
    public TimeSpan Timeout { get; set; }
    public int MaxTimeoutAttempts { get; set; }
    public byte[]? LastReadBlock { get; set; }
    public Dictionary<string, string> Options { get; } = new();
    public int BlockSize { get; set; } = 512;
}