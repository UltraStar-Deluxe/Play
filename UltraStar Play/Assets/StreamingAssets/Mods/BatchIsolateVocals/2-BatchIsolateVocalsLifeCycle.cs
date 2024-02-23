using UnityEngine;
using UniInject;
using UnityEngine.UIElements;
using UniRx;
using System;
using System.Collections.Generic;
using System.Linq;

public class BatchIsolateVocalsLifeCycle : IOnLoadMod
{
    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private BatchIsolateVocalsModSettings modSettings;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private JobManager jobManager;

    [Inject]
    private AudioSeparationManager audioSeparationManager;

    private MessageDialogControl batchIsolateVocalsDialogControl;

    public void OnLoadMod()
    {
        Debug.Log($"{nameof(BatchIsolateVocalsLifeCycle)}.OnLoadMod");
        modSettings.OnShowBatchIsolateVocalsDialog = OnShowBatchIsolateVocalsDialog;
    }

    public void OnShowBatchIsolateVocalsDialog()
    {
        if (batchIsolateVocalsDialogControl != null)
        {
            return;
        }
        batchIsolateVocalsDialogControl = uiManager.CreateDialogControl("Batch Isolate Vocals");
        batchIsolateVocalsDialogControl.DialogClosedEventStream
            .Subscribe(_ => batchIsolateVocalsDialogControl = null);

        List<SongMeta> songMetasWithoutVocalsAudio = GetSongMetasWithoutVocalsAudio();
        FillBatchIsolateVocalsDialog(batchIsolateVocalsDialogControl, songMetasWithoutVocalsAudio);
    }

    private List<SongMeta> GetSongMetasWithoutVocalsAudio()
    {
        Debug.Log($"{nameof(BatchIsolateVocalsLifeCycle)} - searching songs without vocals audio");
        List<SongMeta> result = songMetaManager.GetSongMetas()
            .Where(songMeta => !SongMetaUtils.VocalsAudioResourceExists(songMeta))
            .ToList();
        Debug.Log($"{nameof(BatchIsolateVocalsLifeCycle)} - found {result.Count} songs without vocals audio");
        return result;
    }

    private void FillBatchIsolateVocalsDialog(MessageDialogControl dialogControl, List<SongMeta> songMetas)
    {
        List<SongMeta> sortedSongMetas = songMetas
            .OrderBy(it => SongMetaUtils.GetArtistDashTitle(it))
            .ToList();

        VisualElement toggleContainer = new VisualElement();
        toggleContainer.AddToClassList("child-mb-1");
        dialogControl.AddVisualElement(toggleContainer);

        List<Toggle> toggles = new List<Toggle>();
        foreach (SongMeta songMeta in sortedSongMetas)
        {
            Toggle toggle = new Toggle();
            toggle.label = SongMetaUtils.GetArtistDashTitle(songMeta);
            toggle.value = false;
            toggle.userData = songMeta;
            
            toggles.Add(toggle);

            toggleContainer.Add(toggle);
        }

        dialogControl.AddButton("Deselect All", _ => toggles.ForEach(it => it.value = false)); 
        dialogControl.AddButton("Select All", _ => toggles.ForEach(it => it.value = true)); 
        dialogControl.AddButton("Start vocals isolation", _ => 
        {
            List<SongMeta> selectedSongMetas = toggles
                .Where(toggle => toggle.value)
                .Select(toggle => toggle.userData as SongMeta)
                .ToList();
            BatchIsolateVocals(selectedSongMetas);
        });
    }

    private void BatchIsolateVocals(List<SongMeta> songMetas)
    {
        if (songMetas.IsNullOrEmpty())
        {
            return;
        }

        Debug.Log($"{nameof(BatchIsolateVocalsLifeCycle)} - Batch isolating vocals of {songMetas.Count} songs");

        Job batchJob = new Job("Batch isolate vocals");
        jobManager.AddJob(batchJob);

        // Create jobs for every song, but only start the first job
        List<Job> audioSeparationJobs = new List<Job>();
        for (int i = 0; i < songMetas.Count; i++)
        {
            SongMeta songMeta = songMetas[i];
            Job audioSeparationJob = new Job($"Isolate vocals of '{SongMetaUtils.GetArtistDashTitle(songMeta)}'", batchJob);
            jobManager.AddJob(audioSeparationJob);
            audioSeparationJobs.Add(audioSeparationJob);
        }
        StartNextSongInBatch(songMetas, audioSeparationJobs, 0);
    }

    private void StartNextSongInBatch(List<SongMeta> songMetas, List<Job> audioSeparationJobs, int i)
    {
        if (i >= songMetas.Count
            || i >= audioSeparationJobs.Count)
        {
            return;
        }
        Debug.Log($"Starting vocals isolation of batch song {i + 1} / {songMetas.Count}.");

        SongMeta songMeta = songMetas[i];
        Job audioSeparationJob = audioSeparationJobs[i];

        audioSeparationManager.ProcessSongMetaAsObservable(songMeta, true, audioSeparationJob)
            // Start next job when finished
            .Subscribe(evt => 
            {
                Debug.Log($"Successfully separated audio of batch song {i + 1} / {songMetas.Count}: {evt}.");
                SongMeta nextSongMeta = i < songMetas.Count
                    ? songMetas[i + 1]
                    : null;
                if (nextSongMeta != null)
                {
                    StartNextSongInBatch(songMetas, audioSeparationJobs, i + 1);
                }
                else 
                {
                    Debug.Log($"Finished batch isolation of vocals.");
                }
            });
    }
}
