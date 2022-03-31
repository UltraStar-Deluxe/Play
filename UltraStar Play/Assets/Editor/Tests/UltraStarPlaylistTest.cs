using NUnit.Framework;
using static UltraStarPlaylistParser;

public class UltraStarPlaylistTest
{
    [Test]
    public void PlaylistTest()
    {
        UltraStarPlaylist playlist = new();
        playlist.AddLineEntry(new UltraStartPlaylistLineEntry("# comment"));
        Assert.IsFalse(playlist.HasSongEntry("The artist", "The title"));
        Assert.AreEqual(1, playlist.GetLines().Length);

        playlist.AddLineEntry(new UltraStartPlaylistSongEntry("The artist", "The title"));
        Assert.IsTrue(playlist.HasSongEntry("The artist", "The title"));
        Assert.AreEqual(2, playlist.GetLines().Length);

        playlist.RemoveSongEntry("The artist", "The title");
        Assert.IsFalse(playlist.HasSongEntry("The artist", "The title"));
        Assert.AreEqual(1, playlist.GetLines().Length);
    }

    [Test]
    public void ParsePlaylistLineTest()
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
