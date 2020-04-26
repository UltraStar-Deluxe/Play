using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649


public class NewVersionChecker : MonoBehaviour, INeedInjection
{
    private static readonly string remoteVersionFileUrl = "http://localhost:8080/examples/version.properties.txt";

    // Static variables, to persist these values across scene changes.
    private static bool isRemoteVersionFileDownloadDone;
    private static string remoteVersionFileContent;

    [InjectedInInspector]
    public TextAsset localVersionTextAsset;

    [InjectedInInspector]
    public GameObject newVersionAvailableDialog;

    [InjectedInInspector]
    public Text newVersionAvailableDialogMessage;

    [InjectedInInspector]
    public Button ignoreThisVersionButton;

    [InjectedInInspector]
    public Button ignoreAllFutureVersionsButton;

    [Inject]
    private Settings settings;

    private UnityWebRequest webRequest;

    void Start()
    {
        if (!isRemoteVersionFileDownloadDone)
        {
            StartRemoteVersionFileDownload();
        }
        else if (!remoteVersionFileContent.IsNullOrEmpty())
        {
            CheckForNewVersion();
        }
    }

    void Update()
    {
        if (!isRemoteVersionFileDownloadDone)
        {
            UpdateRemoteVersionFileDownload();
        }
    }

    private void StartRemoteVersionFileDownload()
    {
        webRequest = UnityWebRequest.Get(remoteVersionFileUrl);
        webRequest.SendWebRequest();
    }

    private void UpdateRemoteVersionFileDownload()
    {
        if (webRequest.isNetworkError || webRequest.isHttpError)
        {
            Debug.LogError("Error downloading version file: " + remoteVersionFileUrl + "\n"
                + webRequest.error);
            isRemoteVersionFileDownloadDone = true;
            return;
        }
        if (webRequest.isDone)
        {
            remoteVersionFileContent = webRequest.downloadHandler.text;
            isRemoteVersionFileDownloadDone = true;

            CheckForNewVersion();
        }
    }

    private void CheckForNewVersion()
    {
        Dictionary<string, string> remoteVersionProperties = PropertiesFileParser.ParseText(remoteVersionFileContent);
        Dictionary<string, string> localVersionProperties = PropertiesFileParser.ParseText(localVersionTextAsset.text);

        remoteVersionProperties.TryGetValue("release", out string remoteRelease);
        if (settings.IgnoredReleases.Contains("all")
            || settings.IgnoredReleases.Contains(remoteRelease))
        {
            // The user did opt-out for notifications about this release or new releases in general.
            Debug.Log("Ignoring new release: " + remoteRelease);
            return;
        }

        localVersionProperties.TryGetValue("release", out string localRelease);

        remoteVersionProperties.TryGetValue("build_timestamp", out string remoteBuildTimeStamp);
        localVersionProperties.TryGetValue("build_timestamp", out string localBuildTimeStamp);

        // Remove all non-digit characters, then compare the resulting numbers
        // This makes it possible to compare version names
        // in multiple formats such as 2020-04-05, 2020-04-05-devbuild, 0.1.5, 2017.04+
        int.TryParse(Regex.Replace(remoteRelease, @"[^\d]", ""), out int remoteReleaseNumber);
        int.TryParse(Regex.Replace(localRelease, @"[^\d]", ""), out int localReleaseNumber);

        int.TryParse(Regex.Replace(remoteBuildTimeStamp, @"[^\d]", ""), out int remoteBuildTimeStampNumber);
        int.TryParse(Regex.Replace(localBuildTimeStamp, @"[^\d]", ""), out int localBuildTimeStampNumber);

        if (localReleaseNumber < remoteReleaseNumber
            || (localReleaseNumber == remoteReleaseNumber
                && localBuildTimeStampNumber < remoteBuildTimeStampNumber))
        {
            ShowNewVersionAvailableDialog(remoteVersionProperties);
        }
    }

    private void ShowNewVersionAvailableDialog(Dictionary<string, string> remoteVersionProperties)
    {
        remoteVersionProperties.TryGetValue("release", out string remoteRelease);
        remoteVersionProperties.TryGetValue("website_link", out string websiteLink);

        // Add callbacks to buttons
        ignoreThisVersionButton.OnClickAsObservable()
            .Subscribe(_ =>
            {
                settings.IgnoredReleases.AddIfNotContains(remoteRelease);
                newVersionAvailableDialog.SetActive(false);
            });

        ignoreAllFutureVersionsButton.OnClickAsObservable()
            .Subscribe(_ =>
            {
                settings.IgnoredReleases.Clear();
                settings.IgnoredReleases.Add("all");
                newVersionAvailableDialog.SetActive(false);
            });

        // Set message of the dialog
        newVersionAvailableDialogMessage.text = $"UltraStar Play {remoteRelease} has been released.\n"
            + $"For more information visit <color=\"red\">{websiteLink}</color>";

        // Show dialog
        newVersionAvailableDialog.SetActive(true);
    }
}
