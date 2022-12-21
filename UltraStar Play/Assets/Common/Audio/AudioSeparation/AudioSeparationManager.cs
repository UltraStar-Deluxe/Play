using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SpleeterSharp;
using UnityEngine;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class AudioSeparationManager : MonoBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void InitOnLoad()
    {
        instance = null;
        lockObject = new();
    }

    private static AudioSeparationManager instance;
    public static AudioSeparationManager Instance
    {
        get
        {
            if (instance == null)
            {
                AudioSeparationManager instanceInScene = GameObjectUtils.FindComponentWithTag<AudioSeparationManager>("AudioSeparationManager");
                if (instanceInScene != null)
                {
                    GameObjectUtils.TryInitSingleInstanceWithDontDestroyOnLoad(ref instance, ref instanceInScene);
                }
            }
            return instance;
        }
    }

    private static object lockObject = new();

    [Inject]
    private Settings settings;

    [Inject]
    private SongMetaManager songMetaManager;

    private readonly List<SongMeta> songMetasToProcess = new();
    private readonly ConcurrentBag<SongMeta> doneSongMetas = new();

    private SongMeta currentlyProcessedSongMeta;
    private Thread songProcessingThread;

    private void Update()
    {
        UpdateSongMetasToProcess();
    }

    private void UpdateSongMetasToProcess()
    {
        // Remove done song metas
        doneSongMetas.ForEach(songMeta => songMetasToProcess.Remove(songMeta));

        // Process song metas one after the other on dedicated thread.
        if (!songMetasToProcess.IsNullOrEmpty()
            && currentlyProcessedSongMeta == null)
        {
            string outputFolder = songMetaManager.GetGeneratedSongFolderAbsolutePath();

            SongMeta songMeta = songMetasToProcess[0];
            songProcessingThread = new Thread(() =>
            {
                lock (lockObject)
                {
                    try
                    {
                        currentlyProcessedSongMeta = songMeta;
                        ProcessSongMeta(songMeta, outputFolder);
                    }
                    finally
                    {
                        doneSongMetas.Add(songMeta);
                        currentlyProcessedSongMeta = null;
                    }
                }
            });
            songProcessingThread.Start();
        }
    }

    private void ProcessSongMeta(SongMeta songMeta, string outputFolder)
    {
        Debug.Log($"Separating voice and instrumental audio from {songMeta}");
        UpdateSpleeterSharpConfig();

        SpleeterParameters spleeterParameters = new();
        spleeterParameters.InputFile = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Mp3);
        spleeterParameters.OutputFolder = outputFolder;
        spleeterParameters.OutputFileCodec = "ogg";

        Debug.Log($"Calling spleeter with parameters {JsonConverter.ToJson(spleeterParameters)}");
        SpleeterResult spleeterResult = SpleeterUtils.Split(spleeterParameters);
        UpdateSongMetaWithSpleeterResult(songMeta, spleeterResult);
    }

    public void QueueSongToSeparateVoiceAndInstrumentalAudio(SongMeta songMeta)
    {
        if (songMeta == null
            || WebRequestUtils.IsHttpOrHttpsUri(songMeta.Mp3))
        {
            // The audio separation needs a local file.
            Debug.Log("Cannot separate voice and instrumental audio. Song does not exist or is not on local file system.");
            return;
        }

        if (songMetasToProcess.Contains(songMeta))
        {
            Debug.Log($"Already added {songMeta} to separating voice and instrumental audio");
        }
        else
        {
            Debug.Log($"Adding {songMeta} to separating voice and instrumental audio");
            songMetasToProcess.AddIfNotContains(songMeta);
        }
    }

    private void UpdateSongMetaWithSpleeterResult(SongMeta songMeta, SpleeterResult spleeterResult)
    {
        if (spleeterResult.ExitCode != 0
            || !spleeterResult.Errors.IsNullOrEmpty())
        {
            Debug.LogError($"Spleeter terminated with exit code {spleeterResult.ExitCode}. Error messages:\n" +
                           $"    - {spleeterResult.Errors.JoinWith("\n    - ")}");
            return;
        }

        if (spleeterResult.WrittenFiles.IsNullOrEmpty())
        {
            Debug.LogError("SpleeterResult.WrittenFiles is empty");
            return;
        }

        // Check voice audio
        string voiceAudioPath = spleeterResult.WrittenFiles
            .FirstOrDefault(filePath => Path.GetFileNameWithoutExtension(filePath) == "vocals");
        if (File.Exists(voiceAudioPath))
        {
            Debug.Log("Voice audio written to: " + voiceAudioPath);
            songMeta.VoiceAudio = voiceAudioPath;
        }
        else
        {
            Debug.LogError($"Voice audio not found. Written files: {spleeterResult.WrittenFiles.ToCsv()}");
        }

        // Check instrumental audio
        string instrumentalAudioPath = spleeterResult.WrittenFiles
            .FirstOrDefault(filePath => Path.GetFileNameWithoutExtension(filePath) == "accompaniment");
        if (File.Exists(instrumentalAudioPath))
        {
            Debug.Log("Instrumental audio written to: " + voiceAudioPath);
            songMeta.InstrumentalAudio = instrumentalAudioPath;
        }
        else
        {
            Debug.LogError($"Instrumental audio not found. Written files: {spleeterResult.WrittenFiles.ToCsv()}");
        }
    }

    private void UpdateSpleeterSharpConfig()
    {
        Debug.Log($"Updating spleeter config");
        SpleeterSharpConfig.Create()
            .SetSpleeterCommand(settings.SongEditorSettings.AudioSeparationCommand)
            .SetIsWindows(PlatformUtils.IsWindows())
            .SetLogAction(message => Debug.Log(message));
    }
}
