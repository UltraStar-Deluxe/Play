using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;

public class CreateSingAlongDataFromAudioFileTest : AbstractPlayModeTest
{
    private static readonly string testFolderPath = $"{Application.dataPath}/Tests/PlayModeTests/CreateSingAlongDataTests/SpecialCharacters-测_ส_Тест_αβγ_🌍_äß_ñ";
    private static string OutputFolderPath => $"{Application.temporaryCachePath}/{nameof(CreateSingAlongDataFromAudioFileTest)}/Output/SpecialCharacters-测_ส_Тест_αβγ_🌍_äß_ñ";

    [SetUp]
    public void RemoveOldOutputFiles()
    {
        DirectoryUtils.Delete(OutputFolderPath, true);
    }

    [UnityTest]
    public IEnumerator ShouldCreateSingAlongData() => ShouldCreateSingAlongDataAsync();
    private async Awaitable ShouldCreateSingAlongDataAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        string audioFileName = "HoliznaCC0 - To Be an Animal - Excerpt - 测_ส_Тест_αβγ_🌍_äß_ñ.ogg";
        string audioFilePath = $"{testFolderPath}/{audioFileName}";
        string txtFilePathToBeCreated = $"{OutputFolderPath}/{Path.GetFileNameWithoutExtension(audioFileName)}";

        Dictionary<EVoiceId,string> voiceIdToDisplayName = new();
        SongMeta songMeta = new UltraStarSongMeta(
            "HoliznaCC0",
            "To Be an Animal",
            300,
            audioFilePath,
            voiceIdToDisplayName);
        songMeta.SetFileInfo(txtFilePathToBeCreated);

        Debug.Log($"Creating sing-along data for '{txtFilePathToBeCreated}'");

        CreateSingAlongSongControl createSingAlongSongControl = new();
        Injector injector = UltraStarPlaySceneInjectionManager.Instance.SceneInjector;
        injector.Inject(createSingAlongSongControl);

        SongMeta createdSongMeta = null;
        try
        {
            createdSongMeta = await createSingAlongSongControl.CreateSingAlongSongAsync(songMeta, false);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Assert.Fail($"Exception was thrown while creating sing-along data for audio file '{audioFileName}'");
        }

        Assert.IsNotNull(createdSongMeta, "Failed to create sing-along data, created song meta is null.");

        Debug.Log($"Created sing-along data for audio file '{audioFileName}'");

        // Audio separation must have been executed, files must have been created.
        Assert.IsTrue(SongMetaUtils.VocalsAudioResourceExists(createdSongMeta), "Vocals audio resource does not exist after creating sing-along data");
        Assert.IsTrue(SongMetaUtils.InstrumentalAudioResourceExists(createdSongMeta), "Instrumental audio resource does not exist after creating sing-along data");

        // Speech recognition must have been executed, some lyrics must have been found.
        string lyrics = SongMetaUtils.GetLyrics(createdSongMeta, EVoiceId.P1);
        Debug.Log("Lyrics from speech recognition: " + lyrics);
        Assert.IsNotEmpty(lyrics, "Missing lyrics after creating sing-along data");
        Assert.That(lyrics, Contains.Substring(" "), "Lyrics should contain a space for different notes");

        // Pitch detection must have been executed, some notes must have been created with different pitch.
        List<Note> createdNotes = SongMetaUtils.GetAllNotes(createdSongMeta);
        Assert.IsNotEmpty(createdNotes, "No notes created after creating sing-along data");

        HashSet<int> pitchOfNotes = createdNotes
            .Select(note => note.MidiNote)
            .ToHashSet();
        Assert.IsTrue(pitchOfNotes.Count > 2, "Not enough different pitch values after creating sing-along data");

        // No txt file should have been created
        Assert.IsTrue(!FileUtils.Exists(txtFilePathToBeCreated));

        // Wait until sing-along data has been created
        JobManager jobManager = JobManager.Instance;
        long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        long maxWaitTimeInMillis = 60000;
        while (!jobManager.AllJobsFinished
               && !TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, maxWaitTimeInMillis))
        {
            await Awaitable.WaitForSecondsAsync(0.1f);
        }

        if (TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, maxWaitTimeInMillis))
        {
            Assert.Fail($"Failed to create sing-along data within {maxWaitTimeInMillis} ms");
        }
    }
}
