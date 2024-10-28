using System.Security.Cryptography;

namespace TftpSharp.Tests.IntegrationTests;

internal class Util
{
    public static string HashData(byte[] data) => Convert.ToHexString(SHA1.HashData(data));
}