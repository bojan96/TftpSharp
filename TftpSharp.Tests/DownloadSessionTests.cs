using System.Net;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Moq;
using TftpSharp.Client;
using TftpSharp.Dns;
using TftpSharp.Exceptions;
using TftpSharp.Packet;
using TftpSharp.TransferChannel;

namespace TftpSharp.Tests;

public class DownloadSessionTests
{
    
    [Fact]
    public async Task DownloadLessThanDefaultBlockSize()
    {
        const int maxTimeoutAttempts = 1;
        const string filename = "test";
        const TransferMode transferMode = TransferMode.Octet;
        const string host = "test";
        const int port = 69;
        const int tid = 0;
        var address = IPAddress.Loopback;
        var payload = Enumerable.Repeat((byte)1, 511).ToArray();

        var resolver = GetMockResolver(host, address);
        var transferMock = new Mock<ITransferChannel>(MockBehavior.Strict);

        var mockSequence = new MockSequence();
        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(p => p.Type == Packet.Packet.PacketType.RRQ && 
                                          ((ReadRequestPacket)p).Filename == filename && ((ReadRequestPacket)p).TransferMode == transferMode),
                new IPEndPoint(address, port),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock.InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new DataPacket(1, payload).Serialize(),
                new IPEndPoint(address, tid)));

        transferMock.InSequence(mockSequence)
            .Setup(ch =>
                ch.SendTftpPacketAsync(
                    It.Is<Packet.Packet>(p =>
                        p.Type == Packet.Packet.PacketType.ACK && ((AckPacket)p).BlockNumber == 1),
                    new IPEndPoint(address, tid), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        transferMock.InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .Returns(() => Task.Delay(2000).ContinueWith(_ =>
                new ITransferChannel.ChannelPacket(Array.Empty<byte>(), new IPEndPoint(address, port))));

        using var memoryStream = new MemoryStream();
        var downloadSession = new DownloadSession(
            host: host, 
            filename: filename, 
            transferMode: transferMode, memoryStream, TimeSpan.FromSeconds(1), null, maxTimeoutAttempts: maxTimeoutAttempts, transferMock.Object, resolver);

        await downloadSession.Start();

        Assert.Equal(payload, memoryStream.ToArray());
    }


    [Fact]
    public async Task DownloadTimeout()
    {
        const int maxTimeoutAttempts = 1;
        const string filename = "test";
        const TransferMode transferMode = TransferMode.Octet;
        const string host = "test";
        const int port = 69;
        const int tid = 0;
        var address = IPAddress.Loopback;

        var resolver = GetMockResolver(host, address);
        var transferMock = new Mock<ITransferChannel>(MockBehavior.Strict);
        transferMock
            .Setup(ch => ch.SendTftpPacketAsync(
            It.Is<Packet.Packet>(p => p.Type == Packet.Packet.PacketType.RRQ && ((ReadRequestPacket)p).Filename == filename && ((ReadRequestPacket)p).TransferMode == transferMode),
            new IPEndPoint(address, port),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Exactly(maxTimeoutAttempts));


        transferMock
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .Returns(() => Task.Delay(2000).ContinueWith(t =>
                new ITransferChannel.ChannelPacket(Array.Empty<byte>(), new IPEndPoint(address, tid))))
            .Verifiable(Times.Exactly(maxTimeoutAttempts));


        using var memoryStream = new MemoryStream();
        var downloadSession = new DownloadSession(
            host: host,
            filename: filename,
            transferMode: transferMode,
            stream: memoryStream, 
            timeout: TimeSpan.FromSeconds(1), 
            blockSize: null, 
            maxTimeoutAttempts:1,
            transferChannel: transferMock.Object,
            hostResolver: resolver);


        await Assert.ThrowsAsync<TftpTimeoutException>(async () => await downloadSession.Start());
        transferMock.Verify();
    }

    [Fact]
    public async Task DownloadBiggerThanDefaultBlockSize()
    {
        const int maxTimeoutAttempts = 1;
        const string filename = "test";
        const TransferMode transferMode = TransferMode.Octet;
        const string host = "test";
        const int port = 69;
        const int tid = 0;
        var address = IPAddress.Loopback;
        var payload = Enumerable.Repeat((byte)1, 512).ToArray();
        var resolver = GetMockResolver(host, address);
        var transferMock = new Mock<ITransferChannel>(MockBehavior.Strict);

        var mockSequence = new MockSequence();
        transferMock.InSequence(mockSequence).Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(
                    p => p.Type == Packet.Packet.PacketType.RRQ &&
                         ((ReadRequestPacket)p).Filename == filename &&
                         ((ReadRequestPacket)p).TransferMode == transferMode), new IPEndPoint(address, port),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock.InSequence(mockSequence).Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new DataPacket(1, payload).Serialize(),
                new IPEndPoint(address, tid)));

        transferMock.InSequence(mockSequence).Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(p =>
                    p.Type == Packet.Packet.PacketType.ACK && ((AckPacket)p).BlockNumber == 1),
                new IPEndPoint(address, tid), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock.InSequence(mockSequence).Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new DataPacket(2, Array.Empty<byte>()).Serialize(),
                new IPEndPoint(address, port)));

        transferMock.InSequence(mockSequence).Setup(ch => ch.SendTftpPacketAsync(It.Is<Packet.Packet>(p =>
            p.Type == Packet.Packet.PacketType.ACK &&
            ((AckPacket)p).BlockNumber == 2), new IPEndPoint(address, tid), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        transferMock.InSequence(mockSequence).Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .Returns(() => Task.Delay(5000).ContinueWith(t =>
                new ITransferChannel.ChannelPacket(Array.Empty<byte>(), new IPEndPoint(address, tid))));


        using var memoryStream = new MemoryStream();
        var downloadSession = new DownloadSession(
            host: host,
            filename: filename,
            transferMode: transferMode,
            stream: memoryStream,
            timeout: TimeSpan.FromSeconds(1),
            blockSize: null,
            maxTimeoutAttempts: maxTimeoutAttempts,
            transferChannel: transferMock.Object,
            hostResolver: resolver);


        await downloadSession.Start();

        Assert.Equal(payload, memoryStream.ToArray());
        transferMock.Verify();
    }

    private IHostResolver GetMockResolver(string host, IPAddress address)
    {
        var mock = new Mock<IHostResolver>(MockBehavior.Strict);
        mock.Setup(resolver => resolver.ResolveHostToIpv4AddressAsync(host, default)).ReturnsAsync(address);

        return mock.Object;
    }
}