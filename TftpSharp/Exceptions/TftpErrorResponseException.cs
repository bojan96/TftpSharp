namespace TftpSharp.Exceptions
{
    public class TftpErrorResponseException : TftpException
    {
        public ErrorCode ErrorCode { get; }
        public string ErrorMessage { get; }

        public TftpErrorResponseException(ErrorCode errorCode, string errorMessage) : base($"{(int)errorCode}-{errorMessage}")
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }
    }
}
