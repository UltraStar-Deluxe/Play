using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;

public class EditAndSaveTest : AbstractPlayModeTest
{
    protected static readonly string testFolderPath = Application.dataPath + "/Editor/Tests/TestSongs";

    [UnityTest]
    public IEnumerator CanEditSongAndSaveChanges()
    {
        LogAssert.ignoreFailingMessages = true;

        // Load SongMeta from temporary file
        string testSongFolderName = "SongEditorTestSongs";
        string testSongFolderPath = $"{testFolderPath}/{testSongFolderName}";
        string testSongMetaFileName = "EditSongMeta.txt";
        string originalSongMetaPath = $"{testSongFolderPath}/{testSongMetaFileName}";
        SongMeta originalSongMeta = new LazyLoadedFromFileSongMeta(originalSongMetaPath);

        string tmpSongFolderPath = ApplicationUtils.GetTemporaryCachePath($"SongEditorTest/{testSongFolderName}");
        DirectoryUtils.CopyAll(testSongFolderPath, tmpSongFolderPath);
        string copiedSongMetaPath = $"{tmpSongFolderPath}/{testSongMetaFileName}";
        SongMeta editedSongMeta = new LazyLoadedFromFileSongMeta(copiedSongMetaPath);

        if (!FileUtils.Exists($"{tmpSongFolderPath}/ogg.ogg"))
        {
            throw new FileNotFoundException($"ogg.ogg not found in folder '{tmpSongFolderPath}'");
        }

        // Open song editor
        Injector injector = null;
        UltraStarPlaySceneInjectionManager.SceneInjectionFinishedEventStream
            .SubscribeOneShot(evt => injector = evt.SceneInjector);
        SceneNavigator.Instance.LoadScene(EScene.SongEditorScene, new SongEditorSceneData()
        {
            SongMeta = editedSongMeta,
        });

        yield return new WaitForSeconds(0.5f);

        Assert.IsNotNull(injector, "Did not get injector for loaded scene");

        // Edit notes
        int shiftInBeats = 10;
        int shiftInMidiNotes = 24;
        MoveNotesAction moveNotesAction = injector.CreateAndInject<MoveNotesAction>();
        moveNotesAction.MoveNotesHorizontal(shiftInBeats, SongMetaUtils.GetAllNotes(editedSongMeta));
        moveNotesAction.MoveNotesVertical(shiftInMidiNotes, SongMetaUtils.GetAllNotes(editedSongMeta));

        // Assert notes have been shifted horizontally as expected
        Assert.IsTrue(
            SongMetaUtils.GetAllNotes(originalSongMeta)
                .OrderBy(note => note.StartBeat)
                .Select(note => note.StartBeat + shiftInBeats)
                .SequenceEqual(SongMetaUtils.GetAllNotes(editedSongMeta)
                    .OrderBy(note => note.StartBeat)
                    .Select(note => note.StartBeat)));

        // Assert notes have been shifted vertically
        Assert.IsTrue(
            SongMetaUtils.GetAllNotes(originalSongMeta)
                .OrderBy(note => note.StartBeat)
                .Select(note => note.MidiNote + shiftInMidiNotes)
                .SequenceEqual(SongMetaUtils.GetAllNotes(editedSongMeta)
                    .OrderBy(note => note.StartBeat)
                    .Select(note => note.MidiNote)));

        // Change GAP
        double newVideoGap = 3000;
        SetVideoGapAction setVideoGapAction = injector.CreateAndInject<SetVideoGapAction>();
        setVideoGapAction.Execute(newVideoGap);

        // Assert GAP has been changed as expected
        Assert.AreEqual(newVideoGap, editedSongMeta.VideoGapInMillis, 0.1);

        yield return new WaitForSeconds(0.5f);

        // Save edited song
        SongMetaManager.Instance.SaveSong(editedSongMeta, true);

        // Assert changes have been persisted and loaded as expected
        SongMeta loadedEditedSongMeta = new LazyLoadedFromFileSongMeta(copiedSongMetaPath);
        string expectedJson = JsonConverter.ToJson(editedSongMeta);
        string actualJson = JsonConverter.ToJson(loadedEditedSongMeta);
        Debug.Log($"Expected JSON: {expectedJson}");
        Debug.Log($"Actual JSON: {actualJson}");
        Assert.AreEqual(expectedJson, actualJson);

        // Assert that there is a change compared to the original song
        Assert.AreNotEqual(JsonConverter.ToJson(originalSongMetaPath), JsonConverter.ToJson(loadedEditedSongMeta));
    }
}
