using System;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;

public class SongEditorPitchDetectionControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongMeta songMeta;
    
    [Inject]
    private PitchDetectionManager pitchDetectionManager;

    [Inject]
    private GameObject gameObject;

    private readonly Subject<PitchDetectionFinishedEvent> pitchDetectionFinishedEventStream = new();
    public Subject<PitchDetectionFinishedEvent> PitchDetectionFinishedEventStream => pitchDetectionFinishedEventStream;

    public PitchDetectionResult LastPitchDetectionResult { get; private set; }
    
    public void OnInjectionFinished()
    {
        pitchDetectionManager.PitchDetectionFinishedEventStream
            .Subscribe(evt =>
            {
                SavePitchDetectionResultToFile(evt.SongMeta, evt.PitchDetectionResult);
                pitchDetectionFinishedEventStream.OnNext(evt);
            })
            .AddTo(gameObject);

        // Subscribe to the event stream of this class, because it fires also when loading the PitchDetectionResult from file.
        PitchDetectionFinishedEventStream
            .Subscribe(evt => LastPitchDetectionResult = evt.PitchDetectionResult)
            .AddTo(gameObject);

        RestoreSavedPitchDetectionResult(songMeta);
    }

    private void RestoreSavedPitchDetectionResult(SongMeta theSongMeta)
    {
        string path = GetPitchDetectionResultFilePath(theSongMeta);
        if (!FileUtils.Exists(path))
        {
            return;
        }

        try
        {
            PitchDetectionResult pitchDetectionResult = JsonConverter.FromJson<PitchDetectionResult>(File.ReadAllText(path));
            PitchDetectionFinishedEvent evt = new PitchDetectionFinishedEvent(theSongMeta, pitchDetectionResult);
            Debug.Log($"Loaded PitchDetectionResult from file. path: '{path}'");

            _ = AwaitableUtils.ExecuteAfterDelayInFramesAsync(1, () => pitchDetectionFinishedEventStream.OnNext(evt));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Failed to restore saved PitchDetectionResult file: {e.Message}");
            try
            {
                File.Delete(path);
            }
            catch (Exception deleteException)
            {
                Debug.LogException(deleteException);
                Debug.LogError($"Failed to delete broken PitchDetectionResult file: {e.Message}");
            }
        }
    }

    private void SavePitchDetectionResultToFile(SongMeta theSongMeta, PitchDetectionResult pitchDetectionResult)
    {
        string path = GetPitchDetectionResultFilePath(theSongMeta);
        try
        {
            string json = JsonConverter.ToJson(pitchDetectionResult, false);
            string folder = Path.GetDirectoryName(path);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Failed to save PitchDetectionResult to file: {e.Message}");
        }
    }

    private string GetPitchDetectionResultFilePath(SongMeta theSongMeta)
    {
        string fileName = $"{theSongMeta.Artist}____{theSongMeta.Title}____{GetFileNameHash(theSongMeta)}.json";
        return ApplicationUtils.GetPersistentDataPath($"Cache/SongEditor/PitchDetectionResult/{fileName}");
    }

    private string GetFileNameHash(SongMeta theSongMeta)
    {
        if (theSongMeta.FileInfo != null)
        {
            return HashingUtils.Md5Hash(theSongMeta.FileInfo.FullName);
        }

        return "NO-FILE";
    }
}
