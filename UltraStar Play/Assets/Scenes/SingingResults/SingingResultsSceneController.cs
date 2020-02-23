using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SingingResultsSceneController : MonoBehaviour
{
    public Text songLabel;

    public GameObject onePlayerLayout;
    public GameObject twoPlayerLayout;

    private SingingResultsSceneData sceneData;

    public static SingingResultsSceneController Instance
    {
        get
        {
            return FindObjectOfType<SingingResultsSceneController>();
        }
    }

    void Start()
    {
        sceneData = SceneNavigator.Instance.GetSceneDataOrThrow<SingingResultsSceneData>();
        SelectLayout();
        FillLayout();
    }

    private void FillLayout()
    {
        SongMeta songMeta = sceneData.SongMeta;
        string titleText = (String.IsNullOrEmpty(songMeta.Title)) ? "" : songMeta.Title;
        string artistText = (String.IsNullOrEmpty(songMeta.Artist)) ? "" : " - " + songMeta.Artist;
        songLabel.text = titleText + artistText;

        int i = 0;
        GameObject selectedLayout = GetSelectedLayout();
        foreach (PlayerProfile playerProfile in sceneData.PlayerProfiles)
        {
            SingingResultsSceneData.PlayerScoreData playerScoreData = sceneData.GetPlayerScores(playerProfile);
            SingingResultsPlayerUiController[] uiControllers = selectedLayout.GetComponentsInChildren<SingingResultsPlayerUiController>();
            if (i < uiControllers.Length)
            {
                uiControllers[i].Init(playerProfile, playerScoreData);
            }
            i++;
        }
    }

    private void SelectLayout()
    {
        int playerCount = sceneData.PlayerProfiles.Count;
        List<GameObject> layouts = new List<GameObject>();
        layouts.Add(onePlayerLayout);
        layouts.Add(twoPlayerLayout);

        GameObject selectedLayout = GetSelectedLayout();
        foreach (GameObject layout in layouts)
        {
            layout.SetActive(layout == selectedLayout);
        }
    }

    private GameObject GetSelectedLayout()
    {
        int playerCount = sceneData.PlayerProfiles.Count;
        if (playerCount == 2)
        {
            return twoPlayerLayout;
        }
        return onePlayerLayout;
    }

    public void FinishScene()
    {
        SongSelectSceneData songSelectSceneData = new SongSelectSceneData();
        songSelectSceneData.SongMeta = sceneData.SongMeta;

        SceneNavigator.Instance.LoadScene(EScene.SongSelectScene, songSelectSceneData);
    }
}
