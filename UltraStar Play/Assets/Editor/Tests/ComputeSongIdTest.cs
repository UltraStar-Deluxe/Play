using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ComputeSongIdTest
{
    private static readonly string folderPath = $"{Application.dataPath}/Editor/Tests/TestSongs";

    [Test]
    public void ScoreRelevantSongHashDidNotChange()
    {
        string originalFilePath = $"{folderPath}/ScoreRelevantSongHash.txt";
        UltraStarSongMeta songMeta = LoadSong(originalFilePath);
        string computeScoreRelevantSongHash = SongMetaUtils.ComputeScoreRelevantSongHash(songMeta);

        Assert.AreEqual("881be91e5214e6b381d467595789c5a9", computeScoreRelevantSongHash,
            "ScoreRelevantSongHash calculation changed. " +
            "If this was intended then make sure to update the score database version and compatibility.");
    }

    private UltraStarSongMeta LoadSong(string path)
    {
        return UltraStarSongParser.ParseFile(path, out List<SongIssue> songIssues, null, true);
    }
}
