using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649


public class NewVersionChecker : MonoBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        isRemoteVersionFileDownloadDone = false;
        remoteVersionFileContent = null;
        dialogWasShown = false;
    }

    private static readonly string remoteVersionFileUrl = "https://raw.githubusercontent.com/UltraStar-Deluxe/Play/master/UltraStar%20Play/Assets/VERSION.txt";

    // Static variables, to persist these values across scene changes.
    private static bool isRemoteVersionFileDownloadDone;
    private static string remoteVersionFileContent;
    private static bool dialogWasShown;

    [InjectedInInspector]
    public TextAsset localVersionTextAsset;

    [InjectedInInspector]
    public VisualTreeAsset newVersionDialogUxml;

    [Inject]
    private Settings settings;

    [Inject]
    private Injector injector;

    [Inject]
    private UIDocument uiDoc;

    private UnityWebRequest webRequest;

    void Start()
    {
        if (dialogWasShown)
        {
            // The dialog has been shown before in this run
            gameObject.SetActive(false);
        }
        else if (!isRemoteVersionFileDownloadDone)
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

        // (localRelease is smaller), or ((localRelease is equal to remoteRelease) and (localBuildTimeStamp is smaller))
        try
        {
            if (CompareVersionString(localRelease, remoteRelease) < 0)
            {
                CreateNewVersionAvailableDialog(remoteVersionProperties);
            }
        }
        catch (CompareVersionException ex)
        {
            Debug.LogException(ex);
        }
    }

    private void CreateNewVersionAvailableDialog(Dictionary<string, string> remoteVersionProperties)
    {
        VisualElement dialogRootVisualElement = newVersionDialogUxml.CloneTree();
        dialogRootVisualElement.AddToClassList("overlay");
        NewVersionDialog newVersionDialog = new NewVersionDialog(dialogRootVisualElement, uiDoc.rootVisualElement, remoteVersionProperties);
        injector.WithRootVisualElement(dialogRootVisualElement).Inject(newVersionDialog);
        dialogWasShown = true;
    }

    /**
     * Compares the versions strings a and b.
     * @return (-1 if a < b), (1 if b < a), (0 if a == b)
     */
    public static int CompareVersionString(string versionA, string versionB)
    {
        MatchCollection aMatches = Regex.Matches(versionA, @"\d+");
        int[] aInts = aMatches.Cast<Match>().Select(match => int.Parse(match.Value)).ToArray();

        MatchCollection bMatches = Regex.Matches(versionB, @"\d+");
        int[] bInts = bMatches.Cast<Match>().Select(match => int.Parse(match.Value)).ToArray();

        // Compare integers from left to right.
        if (aInts.IsNullOrEmpty() || bInts.IsNullOrEmpty())
        {
            // The versions are broken.
            throw new CompareVersionException($"Cannot compare '{versionA}' and '{versionB}'");
        }

        for (int i = 0; i < aInts.Length && i < bInts.Length; i++)
        {
            int aInt = aInts[i];
            int bInt = bInts[i];

            if (aInt < bInt)
            {
                return -1;
            }
            if (aInt > bInt)
            {
                return 1;
            }
        }

        // "1.2.3" is smaller that "1.2.3.5"
        if (aInts.Length < bInts.Length)
        {
            return -1;
        }
        if (aInts.Length > bInts.Length)
        {
            return 1;
        }

        return 0;
    }
}
