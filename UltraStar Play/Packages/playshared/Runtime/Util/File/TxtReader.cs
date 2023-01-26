using System.IO;
using System.Text;
using UnityEngine;
using UtfUnknown;

public static class TxtReader
{
    public static StreamReader GetFileStreamReader(string path, Encoding encoding, bool useUniversalCharsetDetector)
    {
        if (path.IsNullOrEmpty()
            || !File.Exists(path))
        {
            throw new UnityException($"Can not read file. No file exists in specified path: {path}");
        }

        if (encoding == null)
        {
            encoding = useUniversalCharsetDetector
                ? GuessUnknownFileEncodingUsingUniversalCharsetDetector(path)
                : GuessUnicodeFileEncoding(path);
        }
        StreamReader reader = new(path, encoding, true);
        return reader;
    }

    private static Encoding GuessUnicodeFileEncoding(string path)
    {
        byte[] buffer = new byte[5];
        FileStream file = new(path, FileMode.Open, FileAccess.Read);
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

    private static Encoding GuessUnknownFileEncodingUsingUniversalCharsetDetector(string filePath)
    {
        DetectionResult detectionResult = CharsetDetector.DetectFromFile(filePath);
        Encoding encoding = detectionResult.Detected.Encoding;
        string encodingName = detectionResult.Detected.EncodingName;
        float confidence = detectionResult.Detected.Confidence;
        if (encoding != null
            && confidence > 0.6f)
        {
            return encoding;
        }
        Debug.LogWarning($"Could not determine encoding of file '{filePath}' with high confidence, using UTF8 as fallback. Encoding detection result was: encoding name: '{encodingName}', C# object '{encoding}', confidence: {confidence}");
        return Encoding.UTF8;
    }
}
