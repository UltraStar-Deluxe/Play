using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;

public class ForcedAlignmentTest : AbstractPlayModeTest
{
    private static string OutputFolderPath => $"{Application.temporaryCachePath}/{nameof(ForcedAlignmentTest)}/Output";

    [Inject]
    private ForcedAlignmentManager forcedAlignmentManager;
    
    [SetUp]
    public void RemoveOldOutputFiles()
    {
        DirectoryUtils.Delete(OutputFolderPath, true);
    }

    [UnityTest]
    public IEnumerator ShouldAlignLyrics() => ShouldAlignLyricsAsync();
    private async Awaitable ShouldAlignLyricsAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        string audioFileName = "ForcedAlignment-UmlautsTest.ogg";
        string audioFilePath = $"{Application.dataPath}/Tests/PlayModeTests/ForcedAlignmentTest/{audioFileName}";
        string txtFilePath = $"{OutputFolderPath}/ForcedAlignmentTest.txt";
        string lyrics = "Ja, ich weiß, es wär idiotisch, dich zu küssen\nDoch warum hast du so abartig süße Lippen?";
        List<string> expectedWords = new()
        {
            "Ja,", "ich", "weiß,", "es", "wär", "idiotisch,", "dich", "zu", "küssen",
            "Doch", "warum", "hast", "du", "so", "abartig", "süße", "Lippen?"
        };

        Dictionary<EVoiceId, string> voiceIdToDisplayName = new();
        SongMeta songMeta = new UltraStarSongMeta(
            "TestArtist",
            "TestTitle",
            300,
            audioFilePath,
            voiceIdToDisplayName);
        songMeta.SetFileInfo(txtFilePath);

        ForcedAlignmentInput forcedAlignmentInput = await GetForcedAlignmentInput(songMeta, lyrics);
        ForcedAlignmentResult forcedAlignmentResult = await forcedAlignmentManager.ProcessSongMetaJob(
                songMeta,
                forcedAlignmentInput)
            .GetResultAsync();

        CollectionAssert.AreEqual(
            expectedWords,
            forcedAlignmentResult.Words.Select(word => word.Word));
    }

    private static async Awaitable<ForcedAlignmentInput> GetForcedAlignmentInput(SongMeta songMeta, string lyrics)
    {
        AudioClip audioClip = await AudioSampleLoader.Instance.LoadAsAudioClip(SongMetaUtils.GetAudioUri(songMeta));
        float[] monoAudioSamples = AudioSampleUtils.GetAudioSamples(audioClip, 0);

        ForcedAlignmentInput forcedAlignmentInput = new ForcedAlignmentInput(
            lyrics,
            monoAudioSamples,
            0,
            monoAudioSamples.Length - 1,
            audioClip.frequency);
        return forcedAlignmentInput;
    }
}
