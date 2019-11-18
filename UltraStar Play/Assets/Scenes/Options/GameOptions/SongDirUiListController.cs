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

    private void CreateListEntry(string songDir, int indexInList)
    {
        SongDirUiListEntry songDirUiListEntry = Instantiate(listEntryPrefab);
        songDirUiListEntry.transform.SetParent(scrollViewContent);

        songDirUiListEntry.SetSongDir(songDir, indexInList);

        songDirUiListEntry.deleteButton.OnClickAsObservable().Subscribe(_ => DeleteSongDir(indexInList));
        songDirUiListEntry.inputField.OnValueChangedAsObservable().Subscribe(newValue => ChangeSongDir(newValue, indexInList));
    }

    private void ChangeSongDir(string newValue, int indexInList)
    {
        SettingsManager.Instance.Settings.GameSettings.songDirs[indexInList] = newValue;
    }

    private void DeleteSongDir(int indexInList)
    {
        SettingsManager.Instance.Settings.GameSettings.songDirs.RemoveAt(indexInList);
        UpdateListEntries();
    }

    private void CreateNewSongDir()
    {
        SettingsManager.Instance.Settings.GameSettings.songDirs.Add("");
        UpdateListEntries();
    }
}
