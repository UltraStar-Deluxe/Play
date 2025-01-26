using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;
using static UnityEngine.Awaitable;
using static ConditionUtils;
using static SceneConditionTestUtils;
using static VisualElementTestUtils;

public class LoadSongsOnDemandTest : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.SongSelectScene.ToString();

    private const int TotalSongCount = 20;
    private const int InitiallyVisibleSongCount = 4;

    protected override List<string> GetRelativeTestSongFilePaths() => new List<string>();

    protected override void ConfigureTestSettings(TestSettings settings)
    {
        Log.MinimumLogLevel = ELogEventLevel.Debug;

        settings.SongDataFetchType = EFetchType.OnDemand;
        settings.ShowSongIndexInSongSelect = true;

        string testSongFolder = ApplicationUtils.GetTemporaryCachePath($"{nameof(LoadSongsOnDemandTest)}");
        CopyTestSongs(testSongFolder);
        settings.SongDirs = new List<string>{ testSongFolder };
    }

    private void CopyTestSongs(string testSongFolder)
    {
        for (int i = 0; i < TotalSongCount; i++)
        {
            string targetFilePath = $"{testSongFolder}/{i}/SomeArtist - SomeTitle{i}.txt";
            SongMeta songMeta = new UltraStarSongMeta(
                "SomeArtist",
                $"SomeTitle{i}",
                200,
                "audio.ogg",
                new Dictionary<EVoiceId,string>());
            DirectoryUtils.CreateDirectory(Path.GetDirectoryName(targetFilePath));
            UltraStarFormatWriter.WriteFile(targetFilePath, songMeta, UltraStarSongFormatVersion.v110);
        }
    }

    [UnityTest]
    public IEnumerator ShouldLoadSongsOnDemand() => ShouldLoadSongsOnDemandAsync();
    private async Awaitable ShouldLoadSongsOnDemandAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();
        await ExpectSongScanFinished();
        await ExpectSongCountNotLoadedYet(TotalSongCount - InitiallyVisibleSongCount);
        await SelectNextSong();
        await ExpectSongCountNotLoadedYet(TotalSongCount - InitiallyVisibleSongCount - 1);
        await SelectNextSong();
        await SelectNextSong();
        await ExpectSongCountNotLoadedYet(TotalSongCount - InitiallyVisibleSongCount - 3);
        await SelectPreviousSong();
        await SelectPreviousSong();
        await SelectPreviousSong();
        await ExpectSongCountNotLoadedYet(TotalSongCount - InitiallyVisibleSongCount - 3);
    }

    private static async Awaitable ExpectSongScanFinished()
    {
        await WaitForCondition(() => SongMetaManager.Instance.IsSongScanFinished,
            new WaitForConditionConfig { description = "wait for song scan finished" });
    }

    private static async Awaitable ExpectSongCountNotLoadedYet(int count)
    {
        await WaitForCondition(
            () => GetSongsCountNotLoadedYet() == count,
            new WaitForConditionConfig { description = $"expect {count} songs to be not loaded yet, songs not loaded yet: {GetSongsCountNotLoadedYet()}" });
    }

    private async Awaitable SelectNextSong()
    {
        InputFixture.PressAndRelease(Keyboard.rightArrowKey);
        await WaitForSecondsAsync(0.1f);
    }

    private async Awaitable SelectPreviousSong()
    {
        InputFixture.PressAndRelease(Keyboard.leftArrowKey);
        await WaitForSecondsAsync(0.1f);
    }

    private static int GetSongsCountNotLoadedYet()
    {
        return SongMetaManager.Instance
            .GetSongMetas()
            .OfType<LazyLoadedSongMeta>()
            .Count(it => it.LoadSongPhase is LazyLoadedSongMeta.ELoadSongPhase.Pending);
    }

    private static string ReplaceHeaderField(string originalFileContent, string headerName, string newValue)
    {
        return Regex.Replace(originalFileContent, $"#{headerName}:.+", $"#{headerName}:{newValue}");
    }
}
