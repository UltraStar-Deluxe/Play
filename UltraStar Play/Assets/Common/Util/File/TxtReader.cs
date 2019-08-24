using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class TxtReader
{
    public static Encoding GuessFileEncoding(string srcFile)
    {
        Encoding enc = Encoding.Default; //Ansi CodePage
        byte[] buffer = new byte[5];
        FileStream file = new FileStream(srcFile, FileMode.Open, FileAccess.Read);
        file.Read(buffer, 0, 5);
        file.Close();
        if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
        {
            enc = Encoding.UTF8;
        }
        else if (buffer[0] == 0xfe && buffer[1] == 0xff)
        {
            enc = Encoding.Unicode;
        }
        else if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xfe && buffer[3] == 0xff)
        {
            enc = Encoding.UTF32;
        }
        else if (buffer[0] == 0x2b && buffer[1] == 0x2f && buffer[2] == 0x76)
        {
            enc = Encoding.UTF7;
        }
        return enc;
    }

    public static StreamReader GetFileStreamReader(string path)
    {
        return GetFileStreamReader(path, null);
    }
    public static StreamReader GetFileStreamReader(string path, Encoding enc)
    {
        if (path == null || !File.Exists(path))
        {
            throw new UnityException("Can not read file. No file exists in specified path!");
        }
        Encoding guessedEncoding = (enc ?? GuessFileEncoding(path));
        StreamReader reader = new StreamReader(path, guessedEncoding, true);
        return reader;
    }

    // see https://stackoverflow.com/a/37592018
    public static string NormalizeWhiteSpaceForLoop(string input)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }
        int len = input.Length,
            index = 0,
            i = 0;
        var src = input.ToCharArray();
        bool skip = false;
        char ch;
        for (; i < len; i++)
        {
            ch = src[i];
            switch (ch)
            {
                case '\u0020':
                case '\u00A0':
                case '\u1680':
                case '\u2000':
                case '\u2001':
                case '\u2002':
                case '\u2003':
                case '\u2004':
                case '\u2005':
                case '\u2006':
                case '\u2007':
                case '\u2008':
                case '\u2009':
                case '\u200A':
                case '\u202F':
                case '\u205F':
                case '\u3000':
                case '\u2028':
                case '\u2029':
                case '\u0009':
                case '\u000A':
                case '\u000B':
                case '\u000C':
                case '\u000D':
                case '\u0085':
                    if (skip)
                    {
                        continue;
                    }
                    src[index++] = ch;
                    skip = true;
                    continue;
                default:
                    skip = false;
                    src[index++] = ch;
                    continue;
            }
        }

        return new string(src, 0, index);
    }
}
