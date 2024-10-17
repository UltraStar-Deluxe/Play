using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class BatchIsolateVocalsModSettings : IModSettings
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

    private List<Toggle> toggles = new List<Toggle>();

    public List<IModSettingControl> GetModSettingControls()
    {
        List<SongMeta> songMetas = GetSongMetasWithoutVocalsAudio();
        return new List<IModSettingControl>()
        {
            new SongListModSettingControl(songMetas, out toggles),
            new ButtonModSettingControl("Deselect All", _ => OnDeselectAll()),
            new ButtonModSettingControl("Select All", _ => OnSelectAll()),
            new ButtonModSettingControl($"Start Vocals Isolation", _ => OnStartVocalsIsolation()),
        };
    }

    private void OnSelectAll()
    {
        toggles.FindAll(toggle => toggle.enabledSelf).ForEach(toggle => toggle.value = true);
    }

    private void OnDeselectAll()
    {
        toggles.ForEach(toggle => toggle.value = false);
    }

    private void OnStartVocalsIsolation()
    {
        List<SongMeta> selectedSongMetas = toggles
            .Where(toggle => toggle.value)
            .Select(toggle => toggle.userData as SongMeta)
            .ToList();
        BatchIsolateVocals(selectedSongMetas);
    }

    private List<SongMeta> GetSongMetasWithoutVocalsAudio()
    {
        Debug.Log($"BatchIsolateVocals - searching songs without vocals audio");
        List<SongMeta> result = songMetaManager.GetSongMetas()
            .Where(songMeta => !SongMetaUtils.VocalsAudioResourceExists(songMeta))
            .ToList();
        Debug.Log($"BatchIsolateVocals - found {result.Count} songs without vocals audio");
        return result;
    }

    private void BatchIsolateVocals(List<SongMeta> songMetas)
    {
        if (songMetas.IsNullOrEmpty())
        {
            return;
        }

        Debug.Log($"BatchIsolateVocals - Batch isolating vocals of {songMetas.Count} songs");

        Job batchJob = new Job(Translation.Of("Batch isolate vocals"));
        jobManager.AddJob(batchJob);

        // Create jobs for every song, but only start the first job
        List<Job> audioSeparationJobs = new List<Job>();
        for (int i = 0; i < songMetas.Count; i++)
        {
            SongMeta songMeta = songMetas[i];
            Job audioSeparationJob = new Job(Translation.Of($"Isolate vocals of '{SongMetaUtils.GetArtistDashTitle(songMeta)}'"), batchJob);
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
        Debug.Log($"Starting vocals isolation of batch song {i + 1} / {songMetas.Count}: '{SongMetaUtils.GetArtistDashTitle(songMetas[i])}'.");

        SongMeta songMeta = songMetas[i];
        Job audioSeparationJob = audioSeparationJobs[i];

        audioSeparationManager.ProcessSongMetaAsObservable(songMeta, true, audioSeparationJob)
            // Start next job when finished
            .Subscribe(evt =>
            {
                Debug.Log($"Successfully separated audio of batch song {i + 1} / {songMetas.Count}: {evt}.");
                int nextIndex = i + 1;
                if (nextIndex < songMetas.Count)
                {
                    StartNextSongInBatch(songMetas, audioSeparationJobs, nextIndex);
                }
                else 
                {
                    Debug.Log($"Finished batch isolation of vocals.");
                }
            });
    }

    private class SongListModSettingControl : IModSettingControl
    {
        private readonly List<SongMeta> songMetas;
        private readonly List<Toggle> toggles = new List<Toggle>();

        public SongListModSettingControl(List<SongMeta> songMetas, out List<Toggle> outToggles)
        {
            this.songMetas = songMetas;
            outToggles = this.toggles;
        }

        public VisualElement CreateVisualElement()
        {
            toggles.Clear();

            if (songMetas.IsNullOrEmpty())
            {
                return new Label("No songs found without vocals audio.");
            }

            List<SongMeta> sortedSongMetas = songMetas
                .OrderBy(it => SongMetaUtils.GetArtistDashTitle(it))
                .ToList();

            VisualElement toggleContainer = new VisualElement();
            toggleContainer.AddToClassList("child-mb-1");

            foreach (SongMeta songMeta in sortedSongMetas)
            {
                Toggle toggle = new Toggle();
                toggle.label = SongMetaUtils.GetArtistDashTitle(songMeta);
                toggle.value = false;
                toggle.userData = songMeta;

                if (!IsSongSupported(songMeta))
                {
                    toggle.SetEnabled(false);
                }
                toggles.Add(toggle);
                toggleContainer.Add(toggle);
            }

            return toggleContainer;
        }

        private readonly string[] supportedExtensions = { "wav", "mp3", "ogg", "m4a", "wma", "flac" };

        private bool IsSongSupported(SongMeta songMeta)
        {
            string audio = songMeta.Audio;
            string extension = Path.GetExtension(audio).ToLower();
            if (extension.StartsWith("."))
            {
                extension = extension.Substring(1);
            }
            bool result = Array.Exists(supportedExtensions, ext => ext.Equals(extension));
            Debug.Log($"Checking support for {audio}, extension: {extension}, result: {result}");
            return result;
        }


    }
}
