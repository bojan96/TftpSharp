using System;

namespace TftpSharp.Exceptions
{
    public class TftpException : Exception
    {
        public TftpException(string message) : base(message) { }
    }
}
