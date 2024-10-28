using TftpSharp.Exceptions;

namespace TftpSharp.Tests.IntegrationTests;

public class DownloadTests
{

    [Theory]
    [InlineData("511", "3e063f065f2560f246bfcef505e3e5cd5e7df302")]
    [InlineData("513", "5b96ee235c38186fd0434525f23e8cc3dd0f6fea")]
    public async Task DownloadSuccessfullyWithDefaultBlockSize(string filename, string expectedHash)
    {
        var tftpClient = new TftpClient("localhost");
        var memoryStream = new MemoryStream();

        await tftpClient.DownloadStreamAsync(filename, memoryStream);

        var hash = Util.HashData(memoryStream.ToArray());
        Assert.Equal(expectedHash, hash, true);
    }

    [Fact]
    public async Task DownloadSuccessfullyWithCustomBlockSize()
    {
        var tftpClient = new TftpClient("localhost")
        {
            BlockSize = 1024
        };

        var memoryStream = new MemoryStream();
        await tftpClient.DownloadStreamAsync("1024", memoryStream);

        var hash = Util.HashData(memoryStream.ToArray());
        Assert.Equal("9f1d3e745b390350c25cd526a14fb3743111d155", hash, true);
    }

    [Fact]
    public async Task DownloadNonExistentFile()
    {
        var tftpClient = new TftpClient("localhost")
        {
            BlockSize = 1024
        };

        var memoryStream = new MemoryStream();

        var exception = await Assert.ThrowsAsync<TftpErrorResponseException>(async () =>
        {
            await tftpClient.DownloadStreamAsync("nonexistent", memoryStream);
        });

        Assert.Equal(ErrorCode.FileNotFound, exception.ErrorCode);
    }
}