using System.Linq;
using System.Text;

namespace TftpSharp.Packet
{
    internal class ErrorPacket : Packet
    {
        public enum ErrorCode
        {
            Undefined, 
            FileNotFound, 
            AccessViolation, 
            DiskFullOrAllocationExceeded, 
            IllegalTftpOperation, 
            UnknownTransferId, 
            FileAlreadyExists, 
            NoSuchUser,
        }

        public ErrorCode Code { get; }
        public string ErrorMessage { get; }

        public ErrorPacket(ErrorCode errorCode, string errorMsg) : base(PacketType.ERROR)
        {
            Code = errorCode;
            ErrorMessage = errorMsg;
        }

        public override byte[] Serialize() =>
            new byte[] { 0, 5 }
                .Concat(UshortToBytes((ushort)Code))
                .Concat(Encoding.UTF8.GetBytes(ErrorMessage))
                .Concat(new byte[] { 0 })
                .ToArray();
    }
}
