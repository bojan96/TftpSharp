namespace TftpSharp.Exceptions
{
    public class TftpTimeoutException : TftpException
    {
        public TftpTimeoutException(int totalAttempts) : base($"Did not receive corresponding packet response after {totalAttempts} attempts")
        {
        }
    }
}
