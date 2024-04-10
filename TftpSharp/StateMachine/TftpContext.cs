using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace TftpSharp.StateMachine;

internal class TftpContext
{
    public TftpContext(UdpClient client, Stream stream, string remoteFilename, TransferMode transferMode, int port, IPAddress host)
    {
        Client = client;
        Stream = stream;
        RemoteFilename = remoteFilename;
        TransferMode = transferMode;
        Port = port;
        Host = host;
    }

    public UdpClient Client { get; }
    public Stream Stream { get; }
    public string RemoteFilename { get; }
    public TransferMode TransferMode { get; }
    public int Port { get; }
    public int TransferId { get; set; }
    public IPAddress Host { get; }
    public TimeSpan Timeout { get; set; }
    public int MaxTimeoutAttempts { get; set; } = 5;
    public byte[]? LastReadBlock { get; set; }
    public Dictionary<string, string> Options { get; } = new();
    public int BlockSize { get; set; } = 512;
}