using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Responsible;
using Serilog.Events;
using UnityEngine.TestTools;
using static Responsible.Responsibly;
using static ResponsibleLogAssertUtils;

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
    public IEnumerator ShouldLoadSongsOnDemand() => IgnoreFailingMessages()
        .ContinueWith(ExpectSongScanFinished())
        .ContinueWith(ExpectSongCountNotLoadedYet(TotalSongCount - InitiallyVisibleSongCount))
        .ContinueWith(SelectNextSong())
        .ContinueWith(ExpectSongCountNotLoadedYet(TotalSongCount - InitiallyVisibleSongCount - 1))
        .ContinueWith(SelectNextSong())
        .ContinueWith(SelectNextSong())
        .ContinueWith(ExpectSongCountNotLoadedYet(TotalSongCount - InitiallyVisibleSongCount - 3))
        .ContinueWith(SelectPreviousSong())
        .ContinueWith(SelectPreviousSong())
        .ContinueWith(SelectPreviousSong())
        .ContinueWith(ExpectSongCountNotLoadedYet(TotalSongCount - InitiallyVisibleSongCount - 3))
        .ToYieldInstruction(Executor);

    private static ITestInstruction<object> ExpectSongScanFinished()
        => WaitForCondition("wait for song scan finished", () => SongMetaManager.Instance.IsSongScanFinished)
            .ExpectWithinSeconds(5f);

    private static ITestInstruction<object> ExpectSongCountNotLoadedYet(int count)
        => WaitForCondition($"expect {count} songs to be not loaded yet", () => GetSongsCountNotLoadedYet() == count,
                stateStringBuilder => stateStringBuilder.AddDetails($"songs not loaded yet: {GetSongsCountNotLoadedYet()}"))
    .ExpectWithinSeconds(1f);

    private ITestInstruction<object> SelectNextSong()
        => Do($"select next song",
            () => InputFixture.PressAndRelease(Keyboard.rightArrowKey));

    private ITestInstruction<object> SelectPreviousSong()
        => Do($"select previous song",
            () => InputFixture.PressAndRelease(Keyboard.leftArrowKey));

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
