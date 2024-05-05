using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CreateSongFromTemplateControl : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public TextAsset songTemplateTxtFile;

    [InjectedInInspector]
    public TextAsset songTemplateCover;

    [InjectedInInspector]
    public TextAsset songTemplateBackground;

    [InjectedInInspector]
    public TextAsset songTemplateAudio;

    [InjectedInInspector]
    public TextAsset songTemplateVideo;

    [Inject]
    private Settings settings;

    [Inject]
    private SettingsManager settingsManager;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private UiManager uiManager;

    public void CreateNewSongFromTemplateAndContinueToSongEditor(
        string audioFile,
        string artist,
        string title,
        bool createCover,
        bool createBackground,
        bool createVideo)
    {
        string artistAndTitle = $"{artist} - {title}";

        bool audioFileExists = FileUtils.Exists(audioFile);
        string persistentDataPathSongsFolder = ApplicationUtils.GetPersistentDataPath("Songs");
        string outputFolder;
        if (audioFileExists)
        {
            outputFolder = Path.GetDirectoryName(audioFile);
        }
        else
        {
            string newSongFolderName = GetUniqueFolderName(artistAndTitle, persistentDataPathSongsFolder);
            outputFolder = $"{persistentDataPathSongsFolder}/{newSongFolderName}";
        }

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        string txtFileContent = songTemplateTxtFile.text
            .Replace("${artistTag}", $"#ARTIST:{artist}")
            .Replace("${titleTag}", $"#TITLE:{title}")
            // Do not use Windows line ending
            .Replace("\r", "");

        // Fill folder with template files
        string audioFileName;
        if (audioFileExists)
        {
            audioFileName = Path.GetFileName(audioFile);
        }
        else
        {
            audioFileName = $"{artistAndTitle}.ogg";
            string audioFilePath = $"{outputFolder}/{audioFileName}";
            File.WriteAllBytes(audioFilePath, songTemplateAudio.bytes);
        }
        txtFileContent = txtFileContent.Replace("${audioTag}", $"#MP3:{audioFileName}");

        if (createCover)
        {
            string coverFileName = $"{artistAndTitle} [CO].jpg";
            string coverFilePath = outputFolder + $"/{coverFileName}";
            if (!FileUtils.Exists(coverFilePath))
            {
                File.WriteAllBytes(coverFilePath, songTemplateCover.bytes);
            }
            txtFileContent = txtFileContent.Replace("${coverTag}", $"#COVER:{coverFileName}");
        }
        else
        {
            txtFileContent = txtFileContent.Replace("${coverTag}\n", "");
        }

        if (createBackground)
        {
            string backgroundFileName = $"{artistAndTitle} [BG].jpg";
            string backgroundFilePath = outputFolder + $"/{backgroundFileName}";
            if (!FileUtils.Exists(backgroundFilePath))
            {
                File.WriteAllBytes(backgroundFilePath, songTemplateBackground.bytes);
            }
            txtFileContent = txtFileContent.Replace("${backgroundTag}", $"#BACKGROUND:{backgroundFileName}");
        }
        else
        {
            txtFileContent = txtFileContent.Replace("${backgroundTag}\n", "");
        }

        if (createVideo)
        {
            string videoFileName = $"{artistAndTitle}.webm";
            string videoFilePath = outputFolder + $"/{videoFileName}";
            if (!FileUtils.Exists(videoFilePath))
            {
                File.WriteAllBytes(videoFilePath, songTemplateVideo.bytes);
            }
            txtFileContent = txtFileContent.Replace("${videoTag}", $"#VIDEO:{videoFileName}");
        }
        else
        {
            txtFileContent = txtFileContent.Replace("${videoTag}\n", "");
        }

        string newSongTxtFile = outputFolder + $"/{artistAndTitle}.txt";
        File.WriteAllText(newSongTxtFile, txtFileContent, SettingsUtils.GetEncodingForWritingUltraStarTxtFile(settings));

        // Add song folder to settings if not done yet
        List<string> songDirs = settings.SongDirs;
        if (!audioFileExists
            && !songDirs.Contains(persistentDataPathSongsFolder))
        {
            songDirs.Add(persistentDataPathSongsFolder);
            settingsManager.SaveSettings();
        }

        if (PlatformUtils.IsStandalone)
        {
            ApplicationUtils.OpenDirectory(outputFolder);
        }

        Debug.Log($"Created new song from template in: {outputFolder}");
        NotificationManager.CreateNotification(Translation.Get(R.Messages.songEditor_createdNewSong,
        "path", outputFolder));

        // Reload songs, now with the newly added song.
        string txtFile = FileScannerUtils.ScanForFiles(new List<string> { outputFolder }, new List<string>() { "*.txt" })
            .FirstOrDefault();
        if (txtFile != null)
        {
            SongMeta newSongMeta = new LazyLoadedFromFileSongMeta(txtFile);
            songMetaManager.AddSongMeta(newSongMeta);
            OpenSongEditorScene(newSongMeta, audioFileExists);
        }
    }

    private void OpenSongEditorScene(SongMeta songMeta, bool createSingAlongDataViaAiTools)
    {
        sceneNavigator.LoadScene(EScene.SongEditorScene, new SongEditorSceneData
        {
            PreviousScene = EScene.MainScene,
            SongMeta = songMeta,
            CreateSingAlongDataViaAiTools = createSingAlongDataViaAiTools,
        });
    }

    private static string GetUniqueFolderName(string folderName, string songsPath)
    {
        string absolutePath = songsPath + "/" + folderName;
        if (!Directory.Exists(absolutePath)
            && !File.Exists(absolutePath))
        {
            return folderName;
        }

        for (int i = 2; i < 100000; i++)
        {
            string folderNameWithSuffix = folderName + $" ({i})";
            absolutePath = songsPath + "/" + folderNameWithSuffix;
            if (!Directory.Exists(absolutePath)
                && !File.Exists(absolutePath))
            {
                return folderNameWithSuffix;
            }
        }

        Debug.LogError($"Could not create unique folder name for given name '{folderName}' in folder {songsPath}");
        return folderName;
    }
}
