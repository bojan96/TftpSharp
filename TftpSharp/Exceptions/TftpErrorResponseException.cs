namespace TftpSharp.Exceptions
{
    public class TftpErrorResponseException : TftpException
    {
        public TftpErrorResponseException(ErrorCode errorCode, string errorMessage) : base($"{(int)errorCode}-{errorMessage}")
        {
        }
    }
}
