using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class LoadingSceneController : MonoBehaviour
{
    public Text m_labelStatus;

    void Start()
    {
        foreach (string device in Microphone.devices)
        {
            Debug.Log("Microphone device name: " + device);
        }
        // Load all songs
        SongMetaManager.ScanFiles();
    }

    void Update()
    {
        if (m_labelStatus != null)
        {
            int found = SongMetaManager.GetNumberOfSongsFound();
            int success = SongMetaManager.GetNumberOfSongsSuccess();
            int failed = SongMetaManager.GetNumberOfSongsFailed();

            m_labelStatus.text =
                System.DateTime.Now.ToLongTimeString()
                + Environment.NewLine
                + "Scanned " + (success + failed) + " out of " + found + " possible songs,"
                + Environment.NewLine
                + "of which " + success + " successful and " + failed + " failed."
                + Environment.NewLine;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SceneManager.LoadScene("src/view/base/SMainView");
            m_labelStatus.text = "ooooo";
        }
    }

    void OnGui()
    {
        GUI.Box(new Rect(0, 0, (int)(Screen.width / 2), (int)(Screen.height / 2)), "A rectangle with some text");
        Debug.Log("Just did draw a Rect!");
    }
}
