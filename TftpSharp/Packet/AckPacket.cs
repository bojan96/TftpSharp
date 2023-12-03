using System.Linq;

namespace TftpSharp.Packet
{
    internal class AckPacket : Packet
    {
        public ushort BlockNumber { get; }

        public AckPacket(ushort blockNumber) : base(PacketType.ACK)
        {
            BlockNumber = blockNumber;
        }

        public override byte[] Serialize() => new byte[] { 0, 4 }.Concat(Packet.UshortToBytes(BlockNumber)).ToArray();
    }
}
