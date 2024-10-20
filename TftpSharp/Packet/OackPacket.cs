using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TftpSharp.Packet;

internal class OackPacket : Packet
{
    public IReadOnlyDictionary<string, string> Options { get; }

    public OackPacket(IReadOnlyDictionary<string, string> options) : base(PacketType.OACK)
    {
        Options = options;
    }

    public override byte[] Serialize() => new byte[] { 0, 6 }.Concat(GetOptionBytes()).ToArray();

    private IEnumerable<byte> GetOptionBytes() =>
        Options.SelectMany(opt =>
            Encoding.UTF8.GetBytes(opt.Key)
                .Concat(new byte[] { 0 })
                .Concat(Encoding.UTF8.GetBytes(opt.Value)
                    .Concat(new byte[] { 0 })));
}