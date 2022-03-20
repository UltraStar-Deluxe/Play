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
using ProTrans;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NewVersionChecker : MonoBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        isGetTagsRequestDone = false;
        isGetVersionFileRequestDone = false;
        dialogWasShown = false;
    }

    private static readonly string remoteVersionFileUrlPattern = "https://raw.githubusercontent.com/UltraStar-Deluxe/Play/{branchOrTag}/UltraStar%20Play/Assets/VERSION.txt";
    private static readonly string getTagsUrl = "https://api.github.com/repos/UltraStar-Deluxe/Play/tags";

    // Static variables, to persist these values across scene changes.
    private static bool isGetTagsRequestDone;
    private static bool isGetVersionFileRequestDone;
    private static string tagsJson;
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
    private UIDocument uiDocument;

    private void Start()
    {
        if (dialogWasShown)
        {
            // The dialog has been shown before during this application execution. Do not show again.
            gameObject.SetActive(false);
        }
        else if (!isGetTagsRequestDone)
        {
            StartGetTagsRequest(getTagsUrl);
        }
    }

    private void StartGetTagsRequest(string url)
    {
        Debug.Log($"Getting tags from: {url}");
        UnityWebRequest getTagsWebRequest = UnityWebRequest.Get(url);
        getTagsWebRequest.SendWebRequest()
            .AsAsyncOperationObservable()
            .Subscribe(_ => UpdateGetTagsRequest(getTagsWebRequest),
                exception => Debug.LogException(exception),
                () => UpdateGetTagsRequest(getTagsWebRequest));
    }

    private void UpdateGetTagsRequest(UnityWebRequest webRequest)
    {
        if (isGetTagsRequestDone
            || !webRequest.isDone)
        {
            return;
        }

        isGetTagsRequestDone = true;
        if (webRequest.result is not UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Getting tags from '{webRequest.url}'"
                           + $" has result {webRequest.result}.\n{webRequest.error}");
            return;
        }

        string getTagsResponse = webRequest.downloadHandler.text;
        List<RepositoryTagDto> repositoryTagDtos = JsonConverter.FromJson<List<RepositoryTagDto>>(getTagsResponse);
        if (repositoryTagDtos == null)
        {
            return;
        }

        RepositoryTagDto newestTagDto = FindNewestTagDto(repositoryTagDtos);
        if (newestTagDto == null)
        {
            return;
        }

        StartGetRemoteVersionFileRequest(newestTagDto.name);
    }

    private void StartGetRemoteVersionFileRequest(string branchOrTag)
    {
        string url = GetRemoteVersionFileUrl(branchOrTag);
        Debug.Log($"Getting version file from: {url}");
        UnityWebRequest getVersionFileWebRequest = UnityWebRequest.Get(url);
        getVersionFileWebRequest.SendWebRequest()
            .AsAsyncOperationObservable()
            .Subscribe(_ => UpdateGetRemoteVersionFileRequest(getVersionFileWebRequest),
                exception => Debug.LogException(exception),
                () => UpdateGetRemoteVersionFileRequest(getVersionFileWebRequest));
    }

    private void UpdateGetRemoteVersionFileRequest(UnityWebRequest webRequest)
    {
        if (isGetVersionFileRequestDone
            || !webRequest.isDone)
        {
            return;
        }

        isGetVersionFileRequestDone = true;
        if (webRequest.result is not UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Getting version file from '{webRequest.url}'"
                           + $" has result {webRequest.result}.\n{webRequest.error}");
            return;
        }

        string remoteVersionFileContent = webRequest.downloadHandler.text;
        CheckForNewVersion(remoteVersionFileContent);
    }

    private void CheckForNewVersion(string remoteVersionFileContent)
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

        try
        {
            if (CompareVersionString(localRelease, remoteRelease) < 0)
            {
                // (localRelease is smaller)
                // or ((localRelease is equal to remoteRelease) and (localBuildTimeStamp is smaller))
                CreateNewVersionAvailableDialog(remoteVersionProperties);
            }
            else
            {
                Debug.Log($"No new release available (localRelease: {localRelease}, remoteRelease: {remoteRelease})");
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
        NewVersionDialog newVersionDialog = new NewVersionDialog(dialogRootVisualElement,
            uiDocument.rootVisualElement.Children().First(),
            remoteVersionProperties);
        injector.WithRootVisualElement(dialogRootVisualElement).Inject(newVersionDialog);
        dialogWasShown = true;
    }

    /**
     * Compares the versions strings a and b.
     * @return (-1 if a < b), (+1 if b < a), (0 if a == b)
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

    private string GetRemoteVersionFileUrl(string branchOrTag)
    {
        return remoteVersionFileUrlPattern.Replace("{branchOrTag}", branchOrTag);
    }

    private RepositoryTagDto FindNewestTagDto(IEnumerable<RepositoryTagDto> tagDtos)
    {
        RepositoryTagDto newestTagDto = null;
        foreach (RepositoryTagDto tagDto in tagDtos)
        {
            if (newestTagDto == null
                || CompareVersionString(newestTagDto.name, tagDto.name) < 0)
            {
                newestTagDto = tagDto;
            }
        }

        return newestTagDto;
    }

    private class RepositoryTagDto
    {
        public string name;
    }
}
