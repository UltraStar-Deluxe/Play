using NUnit.Framework;
using static UltraStarPlaylistParser;

public class UltraStarPlaylistTest
{
    [Test]
    public void ParsePlaylistTest()
    {
        UltraStarPlaylist playlist;
        void AssetPlaylistIsLoadedCorrectly(string expectedName)
        {
            Assert.AreEqual(expectedName, playlist.Name);
            Assert.AreEqual(2, playlist.GetSongEntries().Count);
            Assert.AreEqual("Some Artist 01", playlist.GetSongEntries()[0].Artist);
            Assert.AreEqual("Some Title 01", playlist.GetSongEntries()[0].Title);
            Assert.AreEqual("Some Artist 02", playlist.GetSongEntries()[1].Artist);
            Assert.AreEqual("Some Title 02", playlist.GetSongEntries()[1].Title);
        }

        string folder = "Assets/Editor/Tests/TestPlaylists";
        playlist = UltraStarPlaylistParser.ParseFile($"{folder}/ColonSeparatorPlaylist.upl");
        AssetPlaylistIsLoadedCorrectly("ColonSeparatorPlaylist");

        playlist = UltraStarPlaylistParser.ParseFile($"{folder}/DashSeparatorPlaylist.upl");
        AssetPlaylistIsLoadedCorrectly("DashSeparatorPlaylist");

        playlist = UltraStarPlaylistParser.ParseFile($"{folder}/NamedPlaylist.upl");
        AssetPlaylistIsLoadedCorrectly("Custom Playlist Name");
    }

    [Test]
    public void EditPlaylistTest()
    {
        UltraStarPlaylist playlist = new("");
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

        entry = UltraStarPlaylistLineParser.ParseLine("Special characters => ä,ü,ß,~*(')]}³§ - The title");
        Assert.IsInstanceOf<UltraStartPlaylistSongEntry>(entry);
        Assert.AreEqual("Special characters => ä,ü,ß,~*(')]}³§", (entry as UltraStartPlaylistSongEntry).Artist);
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

        entry = UltraStarPlaylistLineParser.ParseLine("#NAME:MyName01");
        Assert.IsInstanceOf<UltraStartPlaylistHeaderEntry>(entry);
        Assert.AreEqual("NAME", (entry as UltraStartPlaylistHeaderEntry).HeaderName);
        Assert.AreEqual("MyName01", (entry as UltraStartPlaylistHeaderEntry).HeaderValue);

        entry = UltraStarPlaylistLineParser.ParseLine("#nAmE :  MyName02");
        Assert.IsInstanceOf<UltraStartPlaylistHeaderEntry>(entry);
        Assert.AreEqual("NAME", (entry as UltraStartPlaylistHeaderEntry).HeaderName);
        Assert.AreEqual("MyName02", (entry as UltraStartPlaylistHeaderEntry).HeaderValue);

        Assert.Throws<UltraStarPlaylistParserException>(() => UltraStarPlaylistLineParser.ParseLine("\"Missing quote - The title"));
        Assert.Throws<UltraStarPlaylistParserException>(() => UltraStarPlaylistLineParser.ParseLine("Missing separator ~ The title"));
    }
}
