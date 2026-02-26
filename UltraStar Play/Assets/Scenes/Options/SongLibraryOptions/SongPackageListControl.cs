using System;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class SongPackageListControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Injector injector;
    
    [Inject]
    private Settings settings;
    
    [Inject]
    private SongLibraryOptionsSceneControl songLibraryOptionsSceneControl;
    
    [Inject(Key = nameof(songPackageCardUi))]
    private VisualTreeAsset songPackageCardUi;
    
    [Inject(UxmlName = R.UxmlNames.defaultSongPackageTemplate)]
    private VisualElement defaultSongPackageTemplate;
    
    [Inject(UxmlName = R.UxmlNames.communitySongPackageList)]
    private VisualElement communitySongPackageList;

    public void OnInjectionFinished()
    {
        CreateDefaultSongPackageCard();
        CreateCommunitySongPackageCards();
    }

    private async void CreateCommunitySongPackageCards()
    {
        communitySongPackageList.Clear();

        try
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get(new Uri(SongLibraryOptionsSceneControl.SongArchiveInfoJsonUrl));
            string response = await WebRequestUtils.GetWebRequestResponseAsync(webRequest);
            List<SongArchiveEntry> songArchiveEntries = JsonConverter.FromJson<List<SongArchiveEntry>>(response);
            foreach (SongArchiveEntry songArchiveEntry in songArchiveEntries)
            {
                CreateCommunitySongPackageCard(songArchiveEntry);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void CreateCommunitySongPackageCard(SongArchiveEntry songArchiveEntry)
    {
        SongPackageCardControl songPackageCardControl = new SongPackageCardControl(
            songArchiveEntry,
            () => DownloadSongArchive(songArchiveEntry));

        VisualElement songPackageCard = songPackageCardUi.CloneTreeAndGetFirstChild();
        songPackageCard.AddToClassList(R.UssClasses.community_pack);
        communitySongPackageList.Add(songPackageCard);

        injector
            .WithRootVisualElement(songPackageCard)
            .Inject(songPackageCardControl);
        songPackageCardControl.ActionTitle = Translation.Get(R.Messages.action_download);
    }

    private async void DownloadSongArchive(SongArchiveEntry songArchiveEntry)
    {
        DownloadSongArchiveUiControl downloadSongArchiveUiControl = await songLibraryOptionsSceneControl.CreateDownloadSongArchiveUiControl();
        downloadSongArchiveUiControl.Url = songArchiveEntry.url;
        downloadSongArchiveUiControl.StartDownload();
    }

    private void CreateDefaultSongPackageCard()
    {
        string demoSongFolder = ApplicationUtils.GetDemoSongFolderAbsolutePath();
        if (!DirectoryUtils.Exists(demoSongFolder))
        {
            defaultSongPackageTemplate.RemoveFromHierarchy();
            return;
        }

        SongPackageCardControl defaultSongPackageCardControl = new SongPackageCardControl(
            new SongArchiveEntry
            {
                name = "Default Songs",
                description =
                    "Two very simple song files without audio to demonstrate the UltraStar txt file format for solo and duet lyrics.",
                url = "",
            },
            () => AddAndEnableSongFolder(demoSongFolder));

        injector
            .WithRootVisualElement(defaultSongPackageTemplate)
            .Inject(defaultSongPackageCardControl);
    }

    private void AddAndEnableSongFolder(string songFolder)
    {
        settings.SongDirs.Add(songFolder);
        settings.DisabledSongFolders.Remove(songFolder);
        songLibraryOptionsSceneControl.UpdateSongFolderList();
    }
}
