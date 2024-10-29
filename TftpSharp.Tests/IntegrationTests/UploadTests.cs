namespace TftpSharp.Tests.IntegrationTests;

public class UploadTests
{

    [Theory]
    [InlineData("511", "3e063f065f2560f246bfcef505e3e5cd5e7df302")]
    [InlineData("513", "5b96ee235c38186fd0434525f23e8cc3dd0f6fea")]
    public async Task UploadSuccesfullyWithDefaultBlockSize(string fileToUpload, string expectedHash)
    {
        var tftpClient = new TftpClient("localhost");
        var memoryStream = new MemoryStream(await File.ReadAllBytesAsync(GetFileUploadPath(fileToUpload)));
        var uploadedFilename = $"{fileToUpload}-uploaded";
        
        await tftpClient.UploadStreamAsync(uploadedFilename, memoryStream);

        var hash = Util.HashData(await File.ReadAllBytesAsync(GetServerRootPath(uploadedFilename)));
        Assert.Equal(expectedHash, hash, true);
    }

    [Fact]
    public async Task UploadSuccesfullyWithCustomBlockSize()
    {
        const string fileToUpload = "1024";
        var tftpClient = new TftpClient("localhost")
        {
            BlockSize = 1024
        };
        var memoryStream = new MemoryStream(await File.ReadAllBytesAsync(GetFileUploadPath(fileToUpload)));
        var uploadedFilename = $"{fileToUpload}-uploaded";

        await tftpClient.UploadStreamAsync(uploadedFilename, memoryStream);

        var hash = Util.HashData(await File.ReadAllBytesAsync(GetServerRootPath(uploadedFilename)));
        Assert.Equal("9f1d3e745b390350c25cd526a14fb3743111d155", hash, true);
    }

    [Fact]
    public async Task UploadSuccesfullyWithNegotiatedSize()
    {
        const string fileToUpload = "1024";
        var tftpClient = new TftpClient("localhost")
        {
            NegotiateSize = true
        };
        var memoryStream = new MemoryStream(await File.ReadAllBytesAsync(GetFileUploadPath(fileToUpload)));
        var uploadedFilename = $"{fileToUpload}_size_test";

        await tftpClient.UploadStreamAsync(uploadedFilename, memoryStream);

        var hash = Util.HashData(await File.ReadAllBytesAsync(GetServerRootPath(uploadedFilename)));
        Assert.Equal("9f1d3e745b390350c25cd526a14fb3743111d155", hash, true);
    }

    [Fact]
    public async Task UploadSuccesfullyWithNegotiatedTimeout()
    {
        const string fileToUpload = "1024";
        var tftpClient = new TftpClient("localhost")
        {
            NegotiateTimeout = true,
        };
        var memoryStream = new MemoryStream(await File.ReadAllBytesAsync(GetFileUploadPath(fileToUpload)));
        var uploadedFilename = $"{fileToUpload}_size_test";

        await tftpClient.UploadStreamAsync(uploadedFilename, memoryStream);

        var hash = Util.HashData(await File.ReadAllBytesAsync(GetServerRootPath(uploadedFilename)));
        Assert.Equal("9f1d3e745b390350c25cd526a14fb3743111d155", hash, true);
    }

    private static string GetServerRootPath(string filename) =>
        Path.Combine("IntegrationTests", "ServerRoot", filename);
    private static string GetFileUploadPath(string filename) =>
        Path.Combine("IntegrationTests", "FilesToUpload", filename);
}