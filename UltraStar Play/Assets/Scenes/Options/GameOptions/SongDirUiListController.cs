using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class SongDirUiListController : MonoBehaviour
{

    public Button addButton;
    public Transform scrollViewContent;
    public SongDirUiListEntry listEntryPrefab;

    void Start()
    {
        UpdateListEntries();
        addButton.OnClickAsObservable().Subscribe(_ => CreateNewSongDir());
    }

    private void UpdateListEntries()
    {
        // Remove old list entries
        foreach (Transform child in scrollViewContent.transform)
        {
            Destroy(child.gameObject);
        }

        // Create new list entries
        List<string> songDirs = new List<string>(SettingsManager.Instance.Settings.GameSettings.songDirs);
        int index = 0;
        foreach (string songDir in songDirs)
        {
            CreateListEntry(songDir, index);
            index++;
        }
    }

    private void CreateListEntry(string songDir, int songDirIndexInList)
    {
        SongDirUiListEntry songDirUiListEntry = Instantiate(listEntryPrefab);
        songDirUiListEntry.transform.SetParent(scrollViewContent);

        songDirUiListEntry.SetSongDir(songDir, songDirIndexInList);

        songDirUiListEntry.deleteButton.OnClickAsObservable().Subscribe(_ => DeleteSongDir(songDir, songDirIndexInList));
        songDirUiListEntry.inputField.OnValueChangedAsObservable().Subscribe(newValue => ChangeSongDir(newValue, songDirIndexInList));
    }

    private void ChangeSongDir(string newValue, int songDirIndexInList)
    {
        SettingsManager.Instance.Settings.GameSettings.songDirs[songDirIndexInList] = newValue;
    }

    private void DeleteSongDir(string songDir, int songDirIndexInList)
    {
        SettingsManager.Instance.Settings.GameSettings.songDirs.RemoveAt(songDirIndexInList);
        UpdateListEntries();
    }

    private void CreateNewSongDir()
    {
        SettingsManager.Instance.Settings.GameSettings.songDirs.Add("");
        UpdateListEntries();
    }
}
