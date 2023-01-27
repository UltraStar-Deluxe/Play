using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class ParseSongFileHeaderTest
{
    private static readonly string folderPath = Application.dataPath + "/Editor/Tests/TestSongs/";

    [Test]
    public void MissingTagNameTest()
    {
        SongMeta songMeta = SongMetaBuilder.ParseFile(folderPath + "TestSong-MissingTagName.txt", out List<SongIssue> songIssues, null, true);
        Assert.That(songIssues.AnyMatch(songIssue => songIssue.Message.ToLowerInvariant().Contains("missing tag name")));
        Assert.NotNull(songMeta);
    }

    [Test]
    public void MissingTagValueTest()
    {
        SongMeta songMeta = SongMetaBuilder.ParseFile(folderPath + "TestSong-MissingTagValue.txt", out List<SongIssue> songIssues, null, true);
        Assert.NotNull(songMeta);
        Assert.IsEmpty(songMeta.Language);
        Assert.AreEqual(0, songMeta.Year);
    }

    [Test]
    public void SpaceAroundTagNameAndValueTest()
    {
        SongMeta songMeta = SongMetaBuilder.ParseFile(folderPath + "TestSong-SpaceAroundTagNameAndValue.txt", out List<SongIssue> songIssues, null, true);
        Assert.NotNull(songMeta);
        Assert.AreEqual("English", songMeta.Language);
        Assert.AreEqual(2022, songMeta.Year);
    }
}
