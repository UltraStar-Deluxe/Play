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
        SongMetaManager.ScanFiles();
    }

    void Update ()
    {
        if(m_labelStatus != null)
        {
            m_labelStatus.text =
                System.DateTime.Now.ToLongTimeString()
                + Environment.NewLine
                + SongMetaManager.GetScanStatus()
                + Environment.NewLine
                + SongMetaManager.GetSongMetas().Count
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
        GUI.Box(new Rect(0, 0, (int)(Screen.width / 2), (int)(Screen.height / 2)), "Song files scan finished");
        Debug.Log("Just did draw a Rect!");
    }
}
