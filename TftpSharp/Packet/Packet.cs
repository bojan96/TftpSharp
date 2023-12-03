using System;
using System.Linq;

namespace TftpSharp.Packet
{
    internal abstract class Packet
    {
        public enum PacketType { RRQ, WRQ, DATA, ACK, ERROR }

        public PacketType Type { get; }

        protected Packet(PacketType type)
        {
            Type = type;
        }

        public abstract byte[] Serialize();

        public static byte[] UshortToBytes(ushort num)
        {
            var result = BitConverter.GetBytes(num);
            return BitConverter.IsLittleEndian ? result.Reverse().ToArray() : result;
        }

    }
}
