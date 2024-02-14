using System.Collections.Generic;

namespace TftpSharp.Packet
{
    internal class WriteRequestPacket : RequestPacket
    {
        public WriteRequestPacket(string filename, TransferMode transferMode, IReadOnlyDictionary<string, string> options)
            : base(PacketType.WRQ, filename, transferMode, options)
        {
        }
    }
}
