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

    private bool songScanFinished;
    private string newSongFolderAbsolutePath;

    private void Update()
    {
        if (!songScanFinished)
        {
            return;
        }

        SongMeta newSongMeta = songMetaManager.FindSongMeta(songMeta => ApplicationUtils.ComparePaths(songMeta.Directory, newSongFolderAbsolutePath) == 0);
        if (newSongMeta != null)
        {
            OpenSongEditorScene(newSongMeta);
        }
    }

    public void CreateNewSongFromTemplateAndContinueToSongEditor()
    {
        if (!newSongFolderAbsolutePath.IsNullOrEmpty())
        {
            // Already created new song
            return;
        }

        string songsPath = ApplicationManager.PersistentSongsPath();

        string newSongFolderName = GetUniqueFolderName("Artist - Title", songsPath);
        newSongFolderAbsolutePath = songsPath + "/" + newSongFolderName;
        if (!Directory.Exists(newSongFolderAbsolutePath))
        {
            Directory.CreateDirectory(newSongFolderAbsolutePath);
        }

        // Fill folder with template files
        string newSongTxtFile = newSongFolderAbsolutePath + "/Artist - Title.txt";
        File.WriteAllBytes(newSongTxtFile, songTemplateTxtFile.bytes);

        string newSongAudioFile = newSongFolderAbsolutePath + "/Artist - Title.ogg";
        File.WriteAllBytes(newSongAudioFile, songTemplateAudio.bytes);

        string newSongCoverFile = newSongFolderAbsolutePath + "/Artist - Title [CO].jpg";
        File.WriteAllBytes(newSongCoverFile, songTemplateCover.bytes);

        string newSongBackgroundFile = newSongFolderAbsolutePath + "/Artist - Title [BG].jpg";
        File.WriteAllBytes(newSongBackgroundFile, songTemplateBackground.bytes);

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
        uiManager.CreateNotificationVisualElement("Reloading songs, please wait...");
        songMetaManager.ReloadSongMetas();
        songMetaManager.SongScanFinishedEventStream
            .Subscribe(_ => songScanFinished = true)
            .AddTo(gameObject);
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
