using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static UltraStarPlaylistParser;

public class UltraStarPlaylistParserTest
{
    [Test]
    public void ParseLineTest()
    {
        UltraStartPlaylistLineEntry entry;
        entry = UltraStarPlaylistLineParser.ParseLine("# this is a comment");
        Assert.IsNotInstanceOf<UltraStartPlaylistSongEntry>(entry);
        Assert.AreEqual("# this is a comment", entry.Line);

        entry = UltraStarPlaylistLineParser.ParseLine("The band-The title");
        Assert.IsInstanceOf<UltraStartPlaylistSongEntry>(entry);
        Assert.AreEqual("The band", (entry as UltraStartPlaylistSongEntry).Artist);
        Assert.AreEqual("The title", (entry as UltraStartPlaylistSongEntry).Title);

        entry = UltraStarPlaylistLineParser.ParseLine("Special characters: ä,ü,ß,~*(')]}³§ - The title");
        Assert.IsInstanceOf<UltraStartPlaylistSongEntry>(entry);
        Assert.AreEqual("Special characters: ä,ü,ß,~*(')]}³§", (entry as UltraStartPlaylistSongEntry).Artist);
        Assert.AreEqual("The title", (entry as UltraStartPlaylistSongEntry).Title);

        entry = UltraStarPlaylistLineParser.ParseLine(" \t  Artist whitespace \t -   Title whitespace  \t");
        Assert.IsInstanceOf<UltraStartPlaylistSongEntry>(entry);
        Assert.AreEqual("Artist whitespace", (entry as UltraStartPlaylistSongEntry).Artist);
        Assert.AreEqual("Title whitespace", (entry as UltraStartPlaylistSongEntry).Title);

        entry = UltraStarPlaylistLineParser.ParseLine("\"Artist quoted\" - \" Title quoted whitespace \"");
        Assert.IsInstanceOf<UltraStartPlaylistSongEntry>(entry);
        Assert.AreEqual("Artist quoted", (entry as UltraStartPlaylistSongEntry).Artist);
        Assert.AreEqual("Title quoted whitespace", (entry as UltraStartPlaylistSongEntry).Title);

        entry = UltraStarPlaylistLineParser.ParseLine("\"Artist quoted\" - The title");
        Assert.IsInstanceOf<UltraStartPlaylistSongEntry>(entry);
        Assert.AreEqual("Artist quoted", (entry as UltraStartPlaylistSongEntry).Artist);
        Assert.AreEqual("The title", (entry as UltraStartPlaylistSongEntry).Title);

        entry = UltraStarPlaylistLineParser.ParseLine("The artist - \"Title quoted\"");
        Assert.IsInstanceOf<UltraStartPlaylistSongEntry>(entry);
        Assert.AreEqual("The artist", (entry as UltraStartPlaylistSongEntry).Artist);
        Assert.AreEqual("Title quoted", (entry as UltraStartPlaylistSongEntry).Title);

        entry = UltraStarPlaylistLineParser.ParseLine("prefix \"The artist\" - \"The title\" - suffix");
        Assert.IsInstanceOf<UltraStartPlaylistSongEntry>(entry);
        Assert.AreEqual("The artist", (entry as UltraStartPlaylistSongEntry).Artist);
        Assert.AreEqual("The title", (entry as UltraStartPlaylistSongEntry).Title);

        entry = UltraStarPlaylistLineParser.ParseLine("Escaped \\\" quote - Escaped \\\\ backslash");
        Assert.IsInstanceOf<UltraStartPlaylistSongEntry>(entry);
        Assert.AreEqual("Escaped \" quote", (entry as UltraStartPlaylistSongEntry).Artist);
        Assert.AreEqual("Escaped \\ backslash", (entry as UltraStartPlaylistSongEntry).Title);

        Assert.Throws<UltraStarPlaylistParserException>(() => UltraStarPlaylistLineParser.ParseLine("\"Missing quote - The title"));
        Assert.Throws<UltraStarPlaylistParserException>(() => UltraStarPlaylistLineParser.ParseLine("Missing separator ~ The title"));
    }
}
