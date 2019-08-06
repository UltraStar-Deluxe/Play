using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml.Linq;
using System;
using UnityEngine.UI;
using UnityEngine.Events;

public class SongSelectionManager : MonoBehaviour
{
    public RectTransform songListContent;
    public RectTransform songButtonPrefab;

    private List<SongMeta> songs;

    public void Start() {
        // TODO: Scanning the files should not be done here.
        // Think of startup procedure and order of events and asynchronous loading.
        SongMetaManager.ScanFiles();
        
        PopulateSongList();
    }

    private void PopulateSongList()
    {
        // Remove old song buttons.
        foreach(RectTransform songButton in songListContent) {
            GameObject.Destroy(songButton.gameObject);
        }

        // Create new song buttons. One for each loaded song.
        var songMetas = SongMetaManager.GetSongMetas();
        foreach(var songMeta in songMetas) {
            AddSongButton(songMeta);
        }
    }

    private void AddSongButton(SongMeta songMeta) {
        var newSongButton = RectTransform.Instantiate(songButtonPrefab);
        newSongButton.SetParent(songListContent);

        newSongButton.GetComponentInChildren<Text>().text = songMeta.Title;
        newSongButton.GetComponent<Button>().onClick.AddListener( () => OnSongButtonClicked(songMeta) );
    }

    private void OnSongButtonClicked(SongMeta songMeta) {
        Debug.Log($"Clicked on song button: {songMeta.Title}");
    }

}
