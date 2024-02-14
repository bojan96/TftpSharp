using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TftpSharp.Packet
{
    internal abstract class RequestPacket : Packet
    {
        public string Filename { get; }
        public TransferMode TransferMode { get; }
        public IReadOnlyDictionary<string, string> Options { get; }

        protected RequestPacket(PacketType packetType, string filename, TransferMode transferMode, IReadOnlyDictionary<string, string> options) : base(packetType)
        {
            Filename = filename;
            TransferMode = transferMode;
            Options = options;
        }

        public override byte[] Serialize() =>
            new byte[] { 0, (byte)Type }
                .Concat(Encoding.UTF8.GetBytes(Filename))
                .Concat(new byte[] { 0 })
                .Concat(Encoding.UTF8.GetBytes(TransferMode.ToString().ToLowerInvariant()))
                .Concat(new byte[] { 0 })
                .Concat(GetOptionBytes())
                .ToArray();

        private IEnumerable<byte> GetOptionBytes() =>
            Options.SelectMany(opt =>
                Encoding.UTF8.GetBytes(opt.Key)
                    .Concat(new byte[] { 0 })
                    .Concat(Encoding.UTF8.GetBytes(opt.Value)
                        .Concat(new byte[] { 0 })));
    }
}
