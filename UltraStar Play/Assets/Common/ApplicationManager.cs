using System;
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

    public List<string> simulatedCommandLineArguments = new List<string>();

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

    public bool HasCommandLineArgument(string argumentName)
    {
        string[] args = GetCommandLineArguments();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], argumentName, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public string GetCommandLineArgument(string argumentName)
    {
        string[] args = GetCommandLineArguments();
        for (int i = 0; i < (args.Length - 1); i++)
        {
            if (string.Equals(args[i], argumentName, StringComparison.InvariantCultureIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return "";
    }

    public string[] GetCommandLineArguments()
    {
        if (Application.isEditor)
        {
            return simulatedCommandLineArguments.ToArray();
        }
        else
        {
#if UNITY_STANDALONE
            return System.Environment.GetCommandLineArgs();
#else
            return Array.Empty<string>();
#endif
        }
    }
}
