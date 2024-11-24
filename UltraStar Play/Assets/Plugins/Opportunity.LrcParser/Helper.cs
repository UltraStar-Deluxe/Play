using System;

namespace Opportunity.LrcParser
{
    internal static class Helper
    {
        public static void CheckString(string paramName, string value, char[] invalidChars)
        {
            var i = value.IndexOfAny(invalidChars);
            if (i >= 0)
                throw new ArgumentException($"Invalid char '{value[i]}' at index: {i}", paramName);
        }
    }
}
