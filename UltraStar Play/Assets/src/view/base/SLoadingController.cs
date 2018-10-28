using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class SLoadingController : MonoBehaviour
{
    public Text m_labelStatus;

    void Start ()
    {
        foreach (string device in Microphone.devices)
        {
            Debug.Log("Name: " + device);
        }
        SongsManager.ScanSongFiles();
    }

    void Update ()
    {
        if(m_labelStatus != null)
        {
            m_labelStatus.text =
                System.DateTime.Now.ToLongTimeString()
                + Environment.NewLine
                + SongsManager.GetSongScanStatus()
                + Environment.NewLine
                + SongsManager.GetSongs().Count
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
        GUI.Box(new Rect(0, 0, Screen.width / 2, Screen.height / 2), "Song files scan finished");
        Debug.Log("Just did draw a Rect!");
    }
}
