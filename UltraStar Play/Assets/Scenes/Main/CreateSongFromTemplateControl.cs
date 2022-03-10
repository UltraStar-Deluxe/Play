using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

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
        string artist,
        string title,
        bool createCover,
        bool createBackground,
        bool createVideo)
    {
        string songsPath = ApplicationManager.PersistentSongsPath();

        string artistAndTitle = $"{artist} - {title}";
        string newSongFolderName = GetUniqueFolderName(artistAndTitle, songsPath);
        string newSongFolderAbsolutePath = songsPath + "/" + newSongFolderName;
        if (!Directory.Exists(newSongFolderAbsolutePath))
        {
            Directory.CreateDirectory(newSongFolderAbsolutePath);
        }

        string txtFileContent = songTemplateTxtFile.text
            .Replace("${artistTag}", $"#ARTIST:{artist}")
            .Replace("${titleTag}", $"#TITLE:{title}")
            // Do not use Windows line ending
            .Replace("\r", "");

        // Fill folder with template files
        string audioFileName = $"{artistAndTitle}.ogg";
        string audioFilePath = newSongFolderAbsolutePath + $"/{audioFileName}";
        File.WriteAllBytes(audioFilePath, songTemplateAudio.bytes);
        txtFileContent = txtFileContent.Replace("${audioTag}", $"#MP3:{audioFileName}");

        if (createCover)
        {
            string coverFileName = $"{artistAndTitle} [CO].jpg";
            string coverFilePath = newSongFolderAbsolutePath + $"/{coverFileName}";
            File.WriteAllBytes(coverFilePath, songTemplateCover.bytes);
            txtFileContent = txtFileContent.Replace("${coverTag}", $"#COVER:{coverFileName}");
        }
        else
        {
            txtFileContent = txtFileContent.Replace("${coverTag}\n", "");
        }

        if (createBackground)
        {
            string backgroundFileName = $"{artistAndTitle} [BG].jpg";
            string backgroundFilePath = newSongFolderAbsolutePath + $"/{backgroundFileName}";
            File.WriteAllBytes(backgroundFilePath, songTemplateBackground.bytes);
            txtFileContent = txtFileContent.Replace("${backgroundTag}", $"#BACKGROUND:{backgroundFileName}");
        }
        else
        {
            txtFileContent = txtFileContent.Replace("${backgroundTag}\n", "");
        }

        if (createVideo)
        {
            string videoFileName = $"{artistAndTitle}.vp8";
            string videoFilePath = newSongFolderAbsolutePath + $"/{videoFileName}";
            File.WriteAllBytes(videoFilePath, songTemplateVideo.bytes);
            txtFileContent = txtFileContent.Replace("${videoTag}", $"#VIDEO:{videoFileName}");
        }
        else
        {
            txtFileContent = txtFileContent.Replace("${videoTag}\n", "");
        }

        string newSongTxtFile = newSongFolderAbsolutePath + $"/{artistAndTitle}.txt";
        File.WriteAllText(newSongTxtFile, txtFileContent);

        // Add song folder to settings if not done yet
        List<string> songDirs = settings.GameSettings.songDirs;
        if (!songDirs.Contains(songsPath))
        {
            songDirs.Add(songsPath);
            settingsManager.Save();
        }

        if (PlatformUtils.IsStandalone)
        {
            // Open newly created song folder in file system browser
            Application.OpenURL("file://" + newSongFolderAbsolutePath);
        }

        string message = $"Created empty song from template: \n{newSongFolderAbsolutePath}";
        Debug.Log(message);
        uiManager.CreateNotificationVisualElement(message);

        // Reload songs, now with the newly added song.
        List<SongMeta> newSongMetas = songMetaManager.LoadNewSongMetasFromFolder(newSongFolderAbsolutePath);
        SongMeta newSongMeta = newSongMetas.FirstOrDefault();
        if (newSongMeta != null)
        {
            OpenSongEditorScene(newSongMeta);
        }
    }

    private void OpenSongEditorScene(SongMeta songMeta)
    {
        sceneNavigator.LoadScene(EScene.SongEditorScene, new SongEditorSceneData
        {
            PreviousScene = EScene.MainScene,
            SelectedSongMeta = songMeta,
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
