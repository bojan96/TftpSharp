using System.Linq;
using System.Text;

namespace TftpSharp.Packet
{
    internal abstract class RequestPacket : Packet
    {
        public string Filename { get; }
        public TransferMode TransferMode { get; }

        protected RequestPacket(PacketType packetType, string filename, TransferMode transferMode) : base(packetType)
        {
            Filename = filename;
            TransferMode = transferMode;
        }

        public override byte[] Serialize() =>
            new byte[] { 0, (byte)Type }
                .Concat(Encoding.UTF8.GetBytes(Filename))
                .Concat(new byte[] { 0 })
                .Concat(Encoding.UTF8.GetBytes(TransferMode.ToString().ToLowerInvariant()))
                .Concat(new byte[] { 0 })
                .ToArray();
        
    }
}
