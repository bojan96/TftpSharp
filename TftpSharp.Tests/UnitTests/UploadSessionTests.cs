using System.Net;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Moq;
using TftpSharp.Client;
using TftpSharp.Dns;
using TftpSharp.Exceptions;
using TftpSharp.Packet;
using TftpSharp.TransferChannel;

namespace TftpSharp.Tests.UnitTests;

public class UploadSessionTests
{

    [Fact]
    public async Task UploadLessThanDefaultBlockSize()
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
                It.Is<Packet.Packet>(p => p.Type == Packet.Packet.PacketType.WRQ &&
                                          ((WriteRequestPacket)p).Filename == filename && ((WriteRequestPacket)p).TransferMode == transferMode),
                new IPEndPoint(address, port),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new AckPacket(0).Serialize(),
                new IPEndPoint(address, tid)));

        transferMock.
            InSequence(mockSequence)
            .Setup(ch =>
                ch.SendTftpPacketAsync(
                    It.Is<Packet.Packet>(p =>
                        p.Type == Packet.Packet.PacketType.DATA && ((DataPacket)p).BlockNumber == 1 && ((DataPacket)p).Data.SequenceEqual(payload)),
                    new IPEndPoint(address, tid), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new AckPacket(1).Serialize(),
                new IPEndPoint(address, tid)));

        using var memoryStream = new MemoryStream(payload);
        var uploadSession = new UploadSession(
            host: host,
            filename: filename,
            transferMode: transferMode,
            stream: memoryStream,
            timeout: TimeSpan.FromSeconds(1),
            blockSize: null,
            maxTimeoutAttempts: maxTimeoutAttempts,
            transferChannel: transferMock.Object,
            hostResolver: resolver);

        await uploadSession.Start();

        Assert.Equal(payload, memoryStream.ToArray());
    }


    [Fact]
    public async Task UploadTimeoutInitialReceiveState()
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
            It.Is<Packet.Packet>(p => p.Type == Packet.Packet.PacketType.WRQ
                                      && ((WriteRequestPacket)p).Filename == filename
                                      && ((WriteRequestPacket)p).TransferMode == transferMode),
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
        var uploadSession = new UploadSession(
            host: host,
            filename: filename,
            transferMode: transferMode,
            stream: memoryStream,
            timeout: TimeSpan.FromSeconds(1),
            blockSize: null,
            maxTimeoutAttempts: maxTimeoutAttempts,
            transferChannel: transferMock.Object,
            hostResolver: resolver);


        await Assert.ThrowsAsync<TftpTimeoutException>(async () => await uploadSession.Start());
        transferMock.Verify();
    }

    // TODO: Fix
    [Fact]
    public async Task UploadTimeoutReceiveState()
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

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(p =>
                    p.Type == Packet.Packet.PacketType.WRQ && ((WriteRequestPacket)p).Filename == filename &&
                    ((WriteRequestPacket)p).TransferMode == transferMode),
                new IPEndPoint(address, port),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new AckPacket(0).Serialize(),
                new IPEndPoint(address, tid)));

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.SendTftpPacketAsync(
            It.Is<Packet.Packet>(p =>
                p.Type == Packet.Packet.PacketType.DATA && ((DataPacket)p).BlockNumber == 1 && ((DataPacket)p).Data.SequenceEqual(payload)),
                new IPEndPoint(address, tid), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .Returns(() => Task.Delay(2000).ContinueWith(t =>
                new ITransferChannel.ChannelPacket(Array.Empty<byte>(), new IPEndPoint(address, tid))));

        using var memoryStream = new MemoryStream(payload);
        var uploadSession = new UploadSession(
            host: host,
            filename: filename,
            transferMode: transferMode,
            stream: memoryStream,
            timeout: TimeSpan.FromSeconds(1),
            blockSize: null,
            maxTimeoutAttempts: maxTimeoutAttempts,
            transferChannel: transferMock.Object,
            hostResolver: resolver);


        await Assert.ThrowsAsync<TftpTimeoutException>(async () => await uploadSession.Start());
    }

    [Fact]
    public async Task UploadBiggerThanDefaultBlockSize()
    {
        const int maxTimeoutAttempts = 1;
        const string filename = "test";
        const TransferMode transferMode = TransferMode.Octet;
        const string host = "test";
        const int port = 69;
        const int tid = 0;
        var address = IPAddress.Loopback;
        var payload = Enumerable.Repeat((byte)1, 1025).ToArray();
        var resolver = GetMockResolver(host, address);
        var transferMock = new Mock<ITransferChannel>(MockBehavior.Strict);

        var mockSequence = new MockSequence();
        transferMock.InSequence(mockSequence).Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(
                    p => p.Type == Packet.Packet.PacketType.WRQ &&
                         ((WriteRequestPacket)p).Filename == filename &&
                         ((WriteRequestPacket)p).TransferMode == transferMode), new IPEndPoint(address, port),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new AckPacket(0).Serialize(),
                new IPEndPoint(address, tid)));

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(p =>
                    p.Type == Packet.Packet.PacketType.DATA && ((DataPacket)p).BlockNumber == 1 && ((DataPacket)p).Data.SequenceEqual(payload.Take(512))),
                new IPEndPoint(address, tid), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new AckPacket(1).Serialize(),
                new IPEndPoint(address, tid)));

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(p =>
                    p.Type == Packet.Packet.PacketType.DATA && ((DataPacket)p).BlockNumber == 2 && ((DataPacket)p).Data.SequenceEqual(payload.Skip(512).Take(512))),
                new IPEndPoint(address, tid), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new AckPacket(2).Serialize(),
                new IPEndPoint(address, tid)));

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(p =>
                    p.Type == Packet.Packet.PacketType.DATA && ((DataPacket)p).BlockNumber == 3 && ((DataPacket)p).Data.SequenceEqual(payload.Skip(1024))),
                new IPEndPoint(address, tid), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new AckPacket(3).Serialize(),
                new IPEndPoint(address, tid)));

        using var memoryStream = new MemoryStream(payload);
        var uploadSession = new UploadSession(
            host: host,
            filename: filename,
            transferMode: transferMode,
            stream: memoryStream,
            timeout: TimeSpan.FromSeconds(1),
            blockSize: null,
            maxTimeoutAttempts: maxTimeoutAttempts,
            transferChannel: transferMock.Object,
            hostResolver: resolver);


        await uploadSession.Start();

        Assert.Equal(payload, memoryStream.ToArray());
    }

    [Fact]
    public async Task ErrorPacketInitialReceiveState()
    {
        const int maxTimeoutAttempts = 1;
        const string filename = "test";
        const TransferMode transferMode = TransferMode.Octet;
        const string host = "test";
        const int port = 69;
        const int tid = 0;
        const ErrorCode errorCode = ErrorCode.FileNotFound;
        const string errorMsg = "Error message";
        var address = IPAddress.Loopback;
        var resolver = GetMockResolver(host, address);
        var transferMock = new Mock<ITransferChannel>(MockBehavior.Strict);


        var mockSequence = new MockSequence();
        transferMock.InSequence(mockSequence).Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(
                    p => p.Type == Packet.Packet.PacketType.WRQ &&
                         ((WriteRequestPacket)p).Filename == filename &&
                         ((WriteRequestPacket)p).TransferMode == transferMode), new IPEndPoint(address, port),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock.InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new ErrorPacket(errorCode, errorMsg).Serialize(),
                new IPEndPoint(address, tid)));

        using var memoryStream = new MemoryStream();
        var uploadSession = new UploadSession(
            host: host,
            filename: filename,
            transferMode: transferMode,
            stream: memoryStream,
            timeout: TimeSpan.FromSeconds(1),
            blockSize: null,
            maxTimeoutAttempts: maxTimeoutAttempts,
            transferChannel: transferMock.Object,
            hostResolver: resolver);


        var exception = await Assert.ThrowsAsync<TftpErrorResponseException>(async () => await uploadSession.Start());
        Assert.Equal(errorCode, exception.ErrorCode);
        Assert.Equal(errorMsg, exception.ErrorMessage);
    }

    [Fact]
    public async Task ErrorPacketReceiveState()
    {
        const int maxTimeoutAttempts = 1;
        const string filename = "test";
        const TransferMode transferMode = TransferMode.Octet;
        const string host = "test";
        const int port = 69;
        const int tid = 0;
        const ErrorCode errorCode = ErrorCode.FileNotFound;
        const string errorMsg = "Error message";
        var address = IPAddress.Loopback;
        var resolver = GetMockResolver(host, address);
        var payload = Enumerable.Repeat((byte)1, 512).ToArray();
        var transferMock = new Mock<ITransferChannel>(MockBehavior.Strict);


        var mockSequence = new MockSequence();
        transferMock.InSequence(mockSequence).Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(
                    p => p.Type == Packet.Packet.PacketType.WRQ &&
                         ((WriteRequestPacket)p).Filename == filename &&
                         ((WriteRequestPacket)p).TransferMode == transferMode), new IPEndPoint(address, port),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock.InSequence(mockSequence).Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new AckPacket(0).Serialize(),
                new IPEndPoint(address, tid)));

        transferMock.InSequence(mockSequence).Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(
                    p => p.Type == Packet.Packet.PacketType.DATA &&
                         ((DataPacket)p).BlockNumber == 1), new IPEndPoint(address, tid),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock.InSequence(mockSequence).Setup(ch => ch.ReceiveFromAddressAsync(address,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new ErrorPacket(errorCode, errorMsg).Serialize(),
                new IPEndPoint(address, tid)));

        using var memoryStream = new MemoryStream();
        var uploadSession = new UploadSession(
            host: host,
            filename: filename,
            transferMode: transferMode,
            stream: memoryStream,
            timeout: TimeSpan.FromSeconds(1),
            blockSize: null,
            maxTimeoutAttempts: maxTimeoutAttempts,
            transferChannel: transferMock.Object,
            hostResolver: resolver);


        var exception = await Assert.ThrowsAsync<TftpErrorResponseException>(async () => await uploadSession.Start());
        Assert.Equal(errorCode, exception.ErrorCode);
        Assert.Equal(errorMsg, exception.ErrorMessage);
    }

    [Fact]
    public async Task UploadCustomBlockSize()
    {
        const int maxTimeoutAttempts = 1;
        const string filename = "test";
        const TransferMode transferMode = TransferMode.Octet;
        const string host = "test";
        const int port = 69;
        const int tid = 0;
        const int blockSize = 1024;
        var address = IPAddress.Loopback;
        var payload = Enumerable.Repeat((byte)1, blockSize - 1).ToArray();

        var resolver = GetMockResolver(host, address);
        var transferMock = new Mock<ITransferChannel>(MockBehavior.Strict);

        var mockSequence = new MockSequence();
        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(p => p.Type == Packet.Packet.PacketType.WRQ &&
                                          ((WriteRequestPacket)p).Filename == filename
                                          && ((WriteRequestPacket)p).TransferMode == transferMode
                                          && ((WriteRequestPacket)p).Options.GetValueOrDefault("blksize") == blockSize.ToString()),
                new IPEndPoint(address, port),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock.InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new OackPacket(new OackPacket.CaseInsensitiveDictionary
                {
                {
                    "blksize", blockSize.ToString()
                }}).Serialize(),
                new IPEndPoint(address, tid)));

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(p =>
                    p.Type == Packet.Packet.PacketType.DATA && ((DataPacket)p).BlockNumber == 1 && ((DataPacket)p).Data.SequenceEqual(payload)),
                new IPEndPoint(address, tid), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new AckPacket(1).Serialize(),
                new IPEndPoint(address, tid)));

        using var memoryStream = new MemoryStream(payload);
        var uploadSession = new UploadSession(
            host: host,
            filename: filename,
            transferMode: transferMode,
            stream: memoryStream,
            timeout: TimeSpan.FromSeconds(1),
            blockSize: blockSize,
            maxTimeoutAttempts: maxTimeoutAttempts,
            transferChannel: transferMock.Object,
            hostResolver: resolver);

        await uploadSession.Start();

        Assert.Equal(payload, memoryStream.ToArray());
    }


    [Fact]
    public async Task UploadLostPacket()
    {
        const int maxTimeoutAttempts = 2;
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
                It.Is<Packet.Packet>(
                    p => p.Type == Packet.Packet.PacketType.WRQ &&
                         ((WriteRequestPacket)p).Filename == filename &&
                         ((WriteRequestPacket)p).TransferMode == transferMode), new IPEndPoint(address, port),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new AckPacket(0).Serialize(),
                new IPEndPoint(address, tid)));

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(p =>
                    p.Type == Packet.Packet.PacketType.DATA && ((DataPacket)p).BlockNumber == 1 && ((DataPacket)p).Data.SequenceEqual(payload)),
                new IPEndPoint(address, tid), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .Returns(() => Task.Delay(2000).ContinueWith(_ => new ITransferChannel.ChannelPacket(Array.Empty<byte>(), new IPEndPoint(address, tid))));

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.SendTftpPacketAsync(
                It.Is<Packet.Packet>(p =>
                    p.Type == Packet.Packet.PacketType.DATA && ((DataPacket)p).BlockNumber == 1 && ((DataPacket)p).Data.SequenceEqual(payload)),
                new IPEndPoint(address, tid), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transferMock
            .InSequence(mockSequence)
            .Setup(ch => ch.ReceiveFromAddressAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ITransferChannel.ChannelPacket(new AckPacket(1).Serialize(),
                new IPEndPoint(address, tid)));

        using var memoryStream = new MemoryStream(payload);
        var uploadSession = new UploadSession(
            host: host,
            filename: filename,
            transferMode: transferMode,
            stream: memoryStream,
            timeout: TimeSpan.FromSeconds(1),
            blockSize: null,
            maxTimeoutAttempts: maxTimeoutAttempts,
            transferChannel: transferMock.Object,
            hostResolver: resolver);


        await uploadSession.Start();

        Assert.Equal(payload, memoryStream.ToArray());
    }

    private IHostResolver GetMockResolver(string host, IPAddress address)
    {
        var mock = new Mock<IHostResolver>(MockBehavior.Strict);
        mock.Setup(resolver => resolver.ResolveHostToIpv4AddressAsync(host, default)).ReturnsAsync(address);

        return mock.Object;
    }
}