namespace TftpSharp.Packet
{
    internal class WriteRequestPacket : RequestPacket
    {
        public WriteRequestPacket(string filename, TransferMode transferMode)
            : base(PacketType.WRQ, filename, transferMode)
        {
        }
    }
}
