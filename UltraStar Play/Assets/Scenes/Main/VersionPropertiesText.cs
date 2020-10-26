using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;

public class VersionPropertiesText : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public TextAsset versionPropertiesTextAsset;

    [InjectedInInspector]
    public Text releaseUiText;

    [InjectedInInspector]
    public Text buildTimeStampUiText;

    void Start()
    {
        Dictionary<string, string> versionProperties = PropertiesFileParser.ParseText(versionPropertiesTextAsset.text);

        // Show the release number (e.g. release date, or some version number)
        versionProperties.TryGetValue("release", out string release);
        releaseUiText.text = "Release: " + release;

        // Show the build timestamp only for development builds
        if (Debug.isDebugBuild)
        {
            versionProperties.TryGetValue("build_timestamp", out string buildTimeStamp);
            buildTimeStampUiText.text = "Build: " + buildTimeStamp;
        }
        else
        {
            buildTimeStampUiText.text = "";
        }
    }
}
