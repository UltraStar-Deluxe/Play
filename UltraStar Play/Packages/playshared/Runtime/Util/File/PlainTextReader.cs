using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UtfUnknown;

public static class PlainTextReader
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

    public static Encoding GuessUnicodeFileEncoding(string path)
    {
        byte[] buffer = new byte[5];
        // Close stream via using statement
        using FileStream file = new(path, FileMode.Open, FileAccess.Read);
        int readByteCount = file.Read(buffer, 0, 5);
        if (readByteCount < 4)
        {
            return Encoding.UTF8;
        }

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

    public static Encoding GuessUnknownFileEncodingUsingUniversalCharsetDetector(string filePath)
    {
        DetectionResult detectionResult = CharsetDetector.DetectFromFile(filePath);
        if (detectionResult == null
            || detectionResult.Detected == null)
        {
            Debug.LogWarning($"Could not determine encoding of file '{filePath}', using UTF8 as fallback. No encoding detection result found");
            return Encoding.UTF8;
        }

        DetectionDetail detailsWithExistingEncoding = detectionResult.Details
            // Ignore macOS specific encodings, these are ancient
            .Where(detectionDetail => !detectionDetail.EncodingName.StartsWith("x-mac-"))
            .FirstOrDefault(detectionDetail => detectionDetail.Encoding != null);
        if (detailsWithExistingEncoding == null)
        {
            Debug.LogWarning($"Could not determine encoding of file '{filePath}' with high confidence, using UTF8 as fallback. Encoding detection result was: encoding name: '{detectionResult.Detected.EncodingName}', C# object '{detectionResult.Detected.Encoding}', confidence: {detectionResult.Detected.Confidence}");
            return Encoding.UTF8;
        }

        Encoding encoding = detailsWithExistingEncoding.Encoding;
        string encodingName = detailsWithExistingEncoding.EncodingName;
        float confidence = detailsWithExistingEncoding.Confidence;
        if (encoding == null
            || confidence < 0.5f)
        {
            Debug.LogWarning($"Could not determine encoding of file '{filePath}' with high confidence, using UTF8 as fallback. Encoding detection result was: encoding name: '{encodingName}', C# object '{encoding}', confidence: {confidence}");
            return Encoding.UTF8;
        }

        return encoding;
    }
}
