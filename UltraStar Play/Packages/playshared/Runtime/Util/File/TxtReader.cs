using System.IO;
using System.Text;
using UnityEngine;
using UtfUnknown;

public static class TxtReader
{
    public static StreamReader GetFileStreamReader(string path, Encoding enc)
    {
        if (path.IsNullOrEmpty()
            || !File.Exists(path))
        {
            throw new UnityException($"Can not read file. No file exists in specified path: {path}");
        }
        Encoding guessedEncoding = (enc ?? GuessFileEncoding(path));
        StreamReader reader = new(path, guessedEncoding, true);
        return reader;
    }

    private static Encoding GuessFileEncoding(string filePath)
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
