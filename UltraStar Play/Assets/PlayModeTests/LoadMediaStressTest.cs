using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LoadMediaStressTest : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.SongSelectScene.ToString();

    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongVideoPlayer songVideoPlayer;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private Settings settings;

    protected override List<string> GetRelativeTestSongFilePaths() => new();

    protected override void ConfigureTestSettings(TestSettings settings)
    {
        Log.MinimumLogLevel = ELogEventLevel.Debug;
        settings.SongDirs = new List<string>()
        {
            "D:/UltraStar-Songs-Prod/UltraStar-Songs-Local",
        };
        settings.SongPreviewDelayInMillis = 0;
        settings.ShowSongIndexInSongSelect = true;
        settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Always;
    }

    [UnityTest]
    [Ignore(reason: "only for manual execution")] // TODO: prepare song folder for unit test
    public IEnumerator ShouldLoadSongsWithoutCrash()
    {
        LogAssertUtils.IgnoreFailingMessages();

        List<SongMeta> songMetas = songMetaManager.GetSongMetas().ToList();
        for (int i = 0; i < songMetas.Count; i++)
        {
            songRouletteControl.SelectNextEntry();
            SongSelectSongEntry entry = songRouletteControl.SelectedEntry as SongSelectSongEntry;
            Debug.Log($"Selected song {entry.SongMeta}");

            yield return new WaitForSeconds(0.1f);
            // yield return new WaitForEndOfFrame();
        }
        yield return null;
    }
}
