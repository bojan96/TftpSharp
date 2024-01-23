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
    public int Timeout { get; set; } = 3000;
    public int MaxTimeoutAttempts { get; set; } = 5;
}