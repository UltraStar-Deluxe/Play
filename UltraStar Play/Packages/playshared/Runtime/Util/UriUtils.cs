using System.Collections.Generic;

namespace Util
{
    public static class UriUtils
    {
        // The URI class escapes certain characters. Replacing these characters is a simple workaround.
        // See RFC 3986 section 2.2 Reserved Characters (January 2005).
        private static Dictionary<string, string> reservedCharactersToPlaceholder = new()
        {
            { "!", "__EXCLAMATION__" },
            { "*", "__ASTERISK__" },
            { "'", "__APOSTROPHE__" },
            { "(", "__OPENBRACE__" },
            { ")", "__CLOSERACE__" },
            { ";", "__SEMICOLON__" },
            { ":", "__COLON__" },
            { "@", "__AT__" },
            { "&", "__AMPERSAND__" },
            { "=", "__EQUALS__" },
            { "+", "__PLUS__" },
            { "$", "__DOLLAR__" },
            { ",", "__COMMA__" },
            { "/", "__SLASH__" },
            { "?", "__QUESTION__" },
            { "[", "__OPENBRACKET__" },
            { "]", "__CLOSEBRACKET__" },
            { " ", "__SPACE__" },
        };

        public static string ReplaceReservedCharactersWithPlaceholders(string path)
        {
            string result = path;
            reservedCharactersToPlaceholder.ForEach(entry =>
                result = result.Replace(entry.Key, entry.Value));
            return result;
        }

        public static string ReplacePlaceholdersWithReservedCharacters(string path)
        {
            string result = path;
            reservedCharactersToPlaceholder.ForEach(entry =>
                result = result.Replace(entry.Value, entry.Key));
            return result;
        }
    }
}
