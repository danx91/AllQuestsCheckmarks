using System;
using System.Linq;

namespace AllQuestsCheckmarks.Helpers
{
    internal static class AQCUtils
    {
        public static bool IsValidMongoID(string s)
        {
            return s is { Length: 24 } && s.All(c => Uri.IsHexDigit(c));
        }
    }
}
