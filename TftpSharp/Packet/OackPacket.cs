using System.Collections.Generic;

namespace TftpSharp.Packet;

internal class OackPacket : Packet
{
    public IReadOnlyDictionary<string, string> Options { get; }

    public OackPacket(IReadOnlyDictionary<string, string> options) : base(PacketType.OACK)
    {
        Options = options;
    }

    public override byte[] Serialize()
    {
        throw new System.NotImplementedException();
    }
}