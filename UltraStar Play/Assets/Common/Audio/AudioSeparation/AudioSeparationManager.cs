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
    static void StaticInit()
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
            string generatedSongFolderAbsolutePath = ApplicationUtils.GetGeneratedSongFolderAbsolutePath();

            SongMeta songMeta = songMetasToProcess[0];
            songProcessingThread = new Thread(() =>
            {
                lock (lockObject)
                {
                    try
                    {
                        currentlyProcessedSongMeta = songMeta;
                        ProcessSongMeta(songMeta, generatedSongFolderAbsolutePath);
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

    private void ProcessSongMeta(SongMeta songMeta, string generatedSongFolderAbsolutePath)
    {
        Debug.Log($"Separating voice and instrumental audio from song: {songMeta}");
        UpdateSpleeterSharpConfig();

        SpleeterParameters spleeterParameters = new();
        spleeterParameters.InputFile = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Mp3);
        spleeterParameters.OutputFolder = generatedSongFolderAbsolutePath;
        spleeterParameters.OutputFileCodec = "ogg";

        Debug.Log($"Calling SpleeterSharp with parameters {JsonConverter.ToJson(spleeterParameters)}");
        SpleeterResult spleeterResult = SpleeterUtils.Split(spleeterParameters);
        UpdateSongMetaWithSpleeterResult(songMeta, generatedSongFolderAbsolutePath, spleeterResult);
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
            Debug.Log($"Already added song to separating voice and instrumental audio: {songMeta}");
        }
        else
        {
            Debug.Log($"Adding song to separating voice and instrumental audio: {songMeta}");
            songMetasToProcess.AddIfNotContains(songMeta);
        }
    }

    private void UpdateSongMetaWithSpleeterResult(SongMeta songMeta, string generatedSongFolderAbsolutePath, SpleeterResult spleeterResult)
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

        // Save the SongMeta if it changed
        bool songMetaChanged = false;

        // Prepare directory to move created audio files.
        string destinationFolder = DirectoryUtils.IsSubDirectory(songMeta.Directory, generatedSongFolderAbsolutePath)
            ? songMeta.Directory
            : ApplicationUtils.GetGeneratedOutputFolderForSourceFilePath(generatedSongFolderAbsolutePath, songMeta.Directory);
        if (!destinationFolder.IsNullOrEmpty()
            && !Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }

        // Check voice audio
        string vocalsAudioPath = spleeterResult.WrittenFiles
            .FirstOrDefault(filePath => Path.GetFileNameWithoutExtension(filePath) == "vocals");
        if (!vocalsAudioPath.IsNullOrEmpty()
            && File.Exists(vocalsAudioPath))
        {
            Debug.Log("Voice audio written to: " + vocalsAudioPath);

            string destinationVocalsAudioPath = destinationFolder + $"/vocals.ogg";
            Debug.Log("Moving voice audio to: " + destinationVocalsAudioPath);
            FileUtils.MoveFileOverwriteIfExists(vocalsAudioPath, destinationVocalsAudioPath);

            songMeta.VocalsAudio = destinationVocalsAudioPath;
            songMetaChanged = true;
        }
        else
        {
            Debug.LogError($"Voice audio not found. Written files: {spleeterResult.WrittenFiles.ToCsv()}");
        }

        // Check instrumental audio
        string instrumentalAudioPath = spleeterResult.WrittenFiles
            .FirstOrDefault(filePath => Path.GetFileNameWithoutExtension(filePath) == "accompaniment");
        if (!instrumentalAudioPath.IsNullOrEmpty()
            && File.Exists(instrumentalAudioPath))
        {
            Debug.Log("Instrumental audio written to: " + instrumentalAudioPath);

            string destinationInstrumentalAudioPath = destinationFolder + "/instrumental.ogg";
            Debug.Log("Moving instrumental audio to: " + destinationInstrumentalAudioPath);
            FileUtils.MoveFileOverwriteIfExists(instrumentalAudioPath, destinationInstrumentalAudioPath);

            songMeta.InstrumentalAudio = destinationInstrumentalAudioPath;
            songMetaChanged = true;
        }
        else
        {
            Debug.LogError($"Instrumental audio not found. Written files: {spleeterResult.WrittenFiles.ToCsv()}");
        }

        // Remove folder that was created by spleeter
        string spleeterOutputFolder = Path.GetDirectoryName(instrumentalAudioPath);
        if (Directory.Exists(spleeterOutputFolder))
        {
            Directory.Delete(spleeterOutputFolder);
        }

        // Save song meta
        if (songMetaChanged)
        {
            songMetaManager.SaveSong(songMeta, true);
        }
    }

    private void UpdateSpleeterSharpConfig()
    {
        Debug.Log($"Updating spleeter config");
        SpleeterSharpConfig.Create()
            .SetSpleeterCommand(settings.SongEditorSettings.AudioSeparationCommand)
            .SetIsWindows(PlatformUtils.IsWindows())
            .SetLogAction(message => Debug.Log($"SpleeterSharp: {message}"));
    }
}
