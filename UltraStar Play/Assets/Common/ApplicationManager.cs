using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplicationManager : MonoBehaviour
{
    public static ApplicationManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<ApplicationManager>("ApplicationManager");
        }
    }

    [Range(5, 60)]
    public int targetFrameRate = 30;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }

    void Update()
    {
        if (Application.targetFrameRate != targetFrameRate)
        {
            Application.targetFrameRate = targetFrameRate;
        }
    }
}
