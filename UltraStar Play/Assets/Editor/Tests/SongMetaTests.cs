using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class SongMetaTests
{
    private static readonly string folderPath = $"{Application.dataPath}/Editor/Tests/TestSongs";

    [Test]
    public void LoadAndSaveSongDoesNotChangeFields()
    {
        string originalFilePath = $"{folderPath}/LoadAndSaveProperties-TestSong.txt";
        UltraStarSongMeta originalSongMeta = LoadSong(originalFilePath);

        string savedFilePath = $"{Application.temporaryCachePath}/LoadAndSaveProperties-TestSong-Saved.txt";
        UltraStarFormatWriter.WriteFile(savedFilePath, originalSongMeta);

        UltraStarSongMeta savedSongMeta = LoadSong(savedFilePath);

        SongMetaAssertUtils.AssertSongMetasAreEqual(originalSongMeta, savedSongMeta);
    }

    [Test]
    public void CopySongMetaValuesTest()
    {
        string originalFilePath = $"{folderPath}/LoadAndSaveProperties-TestSong.txt";
        SongMeta originalSongMeta = LoadSong(originalFilePath);

        SongMeta copiedSongMeta = new UltraStarSongMeta();
        copiedSongMeta.CopyValues(originalSongMeta);

        SongMetaAssertUtils.AssertSongMetasAreEqual(originalSongMeta, copiedSongMeta);
    }

    private UltraStarSongMeta LoadSong(string path)
    {
        return UltraStarSongParser.ParseFile(path, out List<SongIssue> songIssues, null, true);
    }
}
