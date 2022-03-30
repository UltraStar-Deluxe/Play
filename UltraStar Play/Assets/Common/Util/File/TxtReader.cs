using System.IO;
using System.Text;
using UnityEngine;

public static class TxtReader
{
    public static Encoding GuessFileEncoding(string srcFile)
    {
        byte[] buffer = new byte[5];
        FileStream file = new FileStream(srcFile, FileMode.Open, FileAccess.Read);
        file.Read(buffer, 0, 5);
        file.Close();
        if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
        {
            return Encoding.UTF8;
        }
        else if (buffer[0] == 0xfe && buffer[1] == 0xff)
        {
            return Encoding.Unicode;
        }
        else if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xfe && buffer[3] == 0xff)
        {
            return Encoding.UTF32;
        }
        else if (buffer[0] == 0x2b && buffer[1] == 0x2f && buffer[2] == 0x76)
        {
            return Encoding.UTF7;
        }
        return Encoding.UTF8;
    }

    public static StreamReader GetFileStreamReader(string path, Encoding enc)
    {
        if (path == null || !File.Exists(path))
        {
            throw new UnityException($"Can not read file. No file exists in specified path: {path}");
        }
        Encoding guessedEncoding = (enc ?? GuessFileEncoding(path));
        StreamReader reader = new StreamReader(path, guessedEncoding, true);
        return reader;
    }
}
