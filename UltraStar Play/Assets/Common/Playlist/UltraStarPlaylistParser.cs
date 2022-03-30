﻿using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class UltraStarPlaylistParser
{
    public static readonly char separator = '-';

    public static UltraStarPlaylist ParseFile(string path)
    {
        UltraStarPlaylist playlist = new();
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
            StringBuilder artistBuilder = new(line.Length);
            StringBuilder titleBuilder = new(line.Length);
            char lastChar = '0';
            foreach (char c in line)
            {
                if (c == '"' && lastChar != '\\')
                {
                    insideQuote = !insideQuote;
                    if (insideQuote)
                    {
                        // What has been read before for this token is now obsolete
                        GetStringBuilder(targetToken, artistBuilder, titleBuilder)?.Clear();
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
                    GetStringBuilder(targetToken, artistBuilder, titleBuilder)?.Append(c);
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

        private static StringBuilder GetStringBuilder(Token targetToken, StringBuilder artistBuilder, StringBuilder titleBuilder)
        {
            if (targetToken == Token.Artist)
            {
                return artistBuilder;
            }
            else if (targetToken == Token.Title)
            {
                return titleBuilder;
            }
            return null;
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
