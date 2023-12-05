namespace TftpSharp.Exceptions
{
    internal class TftpInvalidPacketException : TftpException
    {
        public TftpInvalidPacketException(string message) : base(message)
        {
        }
    }
}
