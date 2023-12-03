using System;
using System.Linq;

namespace TftpSharp.Packet
{
    internal class DataPacket : Packet
    {
        public ushort BlockNumber { get; }
        public byte[] Data { get; }

        public DataPacket(ushort blockNumber, byte[] data) : base(PacketType.DATA)
        {
            BlockNumber = blockNumber;
            Data = data;
        }

        public override byte[] Serialize() => 
            new byte[] { 0, 3 }.Concat(Packet.UshortToBytes(BlockNumber)).Concat(Data).ToArray();
    }
}
