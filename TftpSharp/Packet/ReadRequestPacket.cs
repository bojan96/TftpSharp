using System.Collections.Generic;

namespace TftpSharp.Packet
{
    internal class ReadRequestPacket : RequestPacket
    {
        public ReadRequestPacket(string filename, TransferMode transferMode, IReadOnlyDictionary<string, string> options) 
            : base(PacketType.RRQ, filename, transferMode, options)
        {
        }
    }
}
