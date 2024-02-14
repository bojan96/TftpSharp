using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TftpSharp.Exceptions;

namespace TftpSharp.Packet
{
    internal static class PacketParser
    {
        public static Packet Parse(byte[] packetBytes)
        {
            if (packetBytes[0] != 0 || !Enum.IsDefined(typeof(Packet.PacketType), packetBytes[1]))
                throw new TftpInvalidPacketException("Packet type invalid");

            var packetType = (Packet.PacketType)packetBytes[1];


            switch (packetType)
            {
                case Packet.PacketType.DATA:
                    if (packetBytes.Length < 4)
                        throw new TftpInvalidPacketException("Unable to parse the packet");
                    return new DataPacket(Packet.BytesToUshort(packetBytes[2..4]), packetBytes[4..]);

                case Packet.PacketType.ACK:
                    if (packetBytes.Length != 4)
                        throw new TftpInvalidPacketException("ACK: Invalid block number");
                    return new AckPacket(Packet.BytesToUshort(packetBytes[2..4]));

                case Packet.PacketType.ERROR:
                    var result = packetBytes[4..].Select((@byte, index) => new { Byte = @byte, Index = index })
                        .FirstOrDefault(b => b.Byte == 0);

                    if (result is null)
                        throw new TftpInvalidPacketException("ERROR: Missing null terminator");

                    return new ErrorPacket((ErrorCode)Packet.BytesToUshort(packetBytes[2..4]),
                        Encoding.UTF8.GetString(packetBytes[4..(result.Index + 4)]));

                case Packet.PacketType.OACK:
                    IEnumerable<byte> bytes = packetBytes.Skip(2);
                    var options = new Dictionary<string, string>()

                    while (bytes.Any())
                    {
                        var optionNameBytes = bytes.TakeWhile(b => b != 0).ToArray();
                        bytes = bytes.Skip(optionNameBytes.Length);
                        var optionValueBytes = bytes.TakeWhile(b => b != 0).ToArray();
                        bytes = bytes.Skip(optionValueBytes.Length);
                        
                        options.Add(Encoding.UTF8.GetString(optionNameBytes), Encoding.UTF8.GetString(optionValueBytes));
                    }

                    return new OackPacket(options);
                default:
                    throw new TftpInvalidPacketException("Invalid packet");
            }
        }
    }
}
