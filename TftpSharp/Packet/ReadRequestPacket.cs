namespace TftpSharp.Packet
{
    internal class ReadRequestPacket : RequestPacket
    {
        public ReadRequestPacket(string filename, TransferMode transferMode) 
            : base(PacketType.RRQ, filename, transferMode)
        {
        }
    }
}
