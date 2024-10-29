using System;
using System.Collections.Generic;

namespace TftpSharp.Util;

internal class CaseInsensitiveDictionary : Dictionary<string, string>
{
    public CaseInsensitiveDictionary() : base(StringComparer.OrdinalIgnoreCase)
    {

    }
}