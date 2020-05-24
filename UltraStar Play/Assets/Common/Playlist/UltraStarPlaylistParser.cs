using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class UltraStarPlaylistParser
{
    public static readonly char separator = '-';

    public static UltraStarPlaylist ParseFile(string path)
    {
        UltraStarPlaylist playlist = new UltraStarPlaylist();
        string[] lines = File.ReadAllLines(path);
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            try
            {
                UltraStartPlaylistLineEntry lineEntry = UltraStarPlaylistLineParser.ParseLine(lines[lineIndex]);
                playlist.AddLineEntry(lineEntry);
            }
            catch (Exception e)
            {
                Debug.LogException(new UltraStarPlaylistParserException($"Error in line {lineIndex + 1} in file '{path}'", e));
            }
        }
        return playlist;
    }

    public static class UltraStarPlaylistLineParser
    {
        private enum Token
        {
            Artist, Title, Separator
        }

        public static UltraStartPlaylistLineEntry ParseLine(string line)
        {
            if (line.Trim().StartsWith("#"))
            {
                // This is a comment
                return new UltraStartPlaylistLineEntry(line);
            }

            // Read the line character by character.
            // Add this character either to the artist or title, depending on the targetToken.
            Token targetToken = Token.Artist;
            bool insideQuote = false;
            StringBuilder artistBuilder = new StringBuilder(line.Length);
            StringBuilder titleBuilder = new StringBuilder(line.Length);
            char lastChar = '0';
            foreach (char c in line)
            {
                if (c == '"' && lastChar != '\\')
                {
                    insideQuote = !insideQuote;
                    if (insideQuote)
                    {
                        // What has been read before for this token is now obsolete
                        if (targetToken == Token.Artist)
                        {
                            artistBuilder.Clear();
                        }
                        else if (targetToken == Token.Title)
                        {
                            titleBuilder.Clear();
                        }
                    }
                    else if (targetToken == Token.Artist)
                    {
                        // The artist token has been completed, the separator comes next.
                        targetToken = Token.Separator;
                    }
                    else if (targetToken == Token.Title)
                    {
                        // The title has been completed, all done.
                        break;
                    }
                }
                else if (!insideQuote && c == separator
                    && (targetToken == Token.Artist || targetToken == Token.Separator))
                {
                    targetToken = Token.Title;
                }
                else if (c != '\\' || (c == '\\' && lastChar == '\\'))
                {
                    switch (targetToken)
                    {
                        case Token.Artist:
                            artistBuilder.Append(c);
                            break;
                        case Token.Title:
                            titleBuilder.Append(c);
                            break;
                    }
                }
                lastChar = c;
            }

            string artist = artistBuilder.ToString().Trim();
            string title = titleBuilder.ToString().Trim();
            if (!artist.IsNullOrEmpty() && !title.IsNullOrEmpty())
            {
                return new UltraStartPlaylistSongEntry(line, artist, title);
            }
            else
            {
                throw new UltraStarPlaylistParserException("Missing artist or title");
            }
        }
    }

    public class UltraStarPlaylistParserException : Exception
    {
        public UltraStarPlaylistParserException(string message)
            : base(message)
        {
        }

        public UltraStarPlaylistParserException(string message, Exception cause)
            : base(message, cause)
        {
        }
    }
}
