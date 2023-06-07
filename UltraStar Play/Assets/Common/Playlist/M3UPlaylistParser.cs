using System;
using System.IO;
using UnityEngine;

public static class M3UPlaylistParser
{
    public static M3UPlaylist ParseFile(string path)
    {
        M3UPlaylist playlist = new(path);
        string[] lines = File.ReadAllLines(path);
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            try
            {
                string audioFilePath = M3UPlaylistLineParser.GetAudioFilePath(lines[lineIndex]);
                if (!audioFilePath.IsNullOrEmpty())
                {
                    playlist.AddAudioFilePath(audioFilePath);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(new M3UPlaylistParserException($"Error in line {lineIndex + 1} in file '{path}'", e));
            }
        }
        return playlist;
    }

    public static class M3UPlaylistLineParser
    {
        public static string GetAudioFilePath(string line)
        {
            if (line.Trim().StartsWith("#"))
            {
                return "";
            }

            try
            {
                // Try to construct a file object. If it works, it is probably a valid file path.
                if (!new FileInfo(line).Extension.IsNullOrEmpty())
                {
                    return line;
                }
            }
            catch (Exception e)
            {
                // Ignore exception. This is probably not a valid file path.
                return "";
            }
            
            return "";
        }
    }
    
    public class M3UPlaylistParserException : Exception
    {
        public M3UPlaylistParserException(string message) : base(message)
        {
        }

        public M3UPlaylistParserException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
