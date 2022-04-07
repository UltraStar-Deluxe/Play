using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public static class PropertiesFileParser
{
    private static readonly KeyValuePair<string, string> emptyKeyValuePair = new KeyValuePair<string, string>("", "");

    public static Dictionary<string, string> ParseFile(string path)
    {
        string text = File.ReadAllText(path);
        return ParseText(text);
    }

    public static Dictionary<string, string> ParseText(string text)
    {
        Dictionary<string, string> map = new Dictionary<string, string>();
        using (StringReader stringReader = new StringReader(text))
        {
            for (string line = stringReader.ReadLine(); line != null; line = stringReader.ReadLine())
            {
                KeyValuePair<string, string> entry = ParseLine(line);
                if (!entry.Equals(emptyKeyValuePair))
                {
                    map.Add(entry.Key, entry.Value);
                }
            }
        }
        return map;
    }

    private static KeyValuePair<string, string> ParseLine(string line)
    {
        if (IsCommentLine(line))
        {
            return emptyKeyValuePair;
        }

        int indexOfEquals = line.IndexOf("=");
        if (indexOfEquals < 0)
        {
            return emptyKeyValuePair;
        }
        string keyPart = line.Substring(0, indexOfEquals);
        string valuePart = line.Substring(indexOfEquals + 1, line.Length - indexOfEquals - 1);

        string trimmedKeyPart = keyPart.Trim();
        string trimmedValuePart = valuePart.Trim();

        string unquotedTrimmedKeyPart = RemoveQuotes(trimmedKeyPart);
        string unquotedTrimmedValuePart = RemoveQuotes(trimmedValuePart);

        string finalValue = ReplaceEscapedCharacters(unquotedTrimmedValuePart);

        return new KeyValuePair<string, string>(unquotedTrimmedKeyPart, finalValue);
    }

    private static string ReplaceEscapedCharacters(string text)
    {
        string result = text.Replace(@"\n", "\n");
        return result;
    }

    private static string RemoveQuotes(string text)
    {
        if (text.StartsWith("\"") && text.EndsWith("\""))
        {
            return text.Substring(1, text.Length - 1);
        }
        else
        {
            return text;
        }
    }

    private static bool IsCommentLine(string line)
    {
        string trimmedLine = line.TrimStart();
        if (trimmedLine.StartsWith("#") || trimmedLine.StartsWith("!"))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
