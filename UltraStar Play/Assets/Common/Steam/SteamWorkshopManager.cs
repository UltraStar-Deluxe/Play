using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Steamworks.Ugc;
using UniInject;
using UniRx;
using UnityEngine;

public class SteamWorkshopManager : AbstractSingletonBehaviour, INeedInjection, IInjectionFinishedListener
{
    public static SteamWorkshopManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SteamWorkshopManager>();

    private readonly ConcurrentBag<Item> downloadedItems = new();
    private readonly UseSteamWorkshopItemsControl useSteamWorkshopItemsControl = new();
    private readonly Subject<bool> finishDownloadWorkshopItemsEventStream = new();
    public IObservable<bool> FinishDownloadWorkshopItemsEventStream => finishDownloadWorkshopItemsEventStream
        .ObserveOnMainThread();

    [Inject]
    private Injector injector;

    public EDownloadState DownloadState { get; private set; } = EDownloadState.Pending;
    public List<Item> DownloadedWorkshopItems
    {
        get
        {
            return downloadedItems
                .Distinct()
                .ToList();
        }
    }

    protected override object GetInstance()
    {
        return Instance;
    }

    public void OnInjectionFinished()
    {
        injector.Inject(useSteamWorkshopItemsControl);
    }

    public void DownloadWorkshopItems()
    {
        downloadedItems.Clear();
        ObservableUtils.RunOnNewTaskAsObservable(async () =>
                await DownloadSubscribedWorkshopItemsAsync())
            .ObserveOnMainThread()
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to download Steam Workshop items: {ex.Message}");
                FireDownloadFinishedEvent();
            })
            .Subscribe(_ =>
            {
                Debug.Log($"Successfully downloaded {downloadedItems.Count} Steam Workshop Items");
                FireDownloadFinishedEvent();
                useSteamWorkshopItemsControl.UseWorkshopItems(DownloadedWorkshopItems);
            });
    }

    private void FireDownloadFinishedEvent()
    {
        try
        {
            finishDownloadWorkshopItemsEventStream.OnNext(true);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to notify subscribers about downloaded workshop items: {ex.Message}");
        }
    }

    private async Task DownloadSubscribedWorkshopItemsAsync()
    {
        if (DownloadState is not EDownloadState.Pending)
        {
            return;
        }

        try
        {
            DownloadState = EDownloadState.Started;
            List<Item> subscribedWorkshopItems = await QuerySubscribedWorkshopItemsAsync();
            Debug.Log($"Downloading or updating {subscribedWorkshopItems.Count} Steam Workshop items");
            await DownloadWorkshopItemsAsync(subscribedWorkshopItems);
        }
        finally
        {
            DownloadState = EDownloadState.Finished;
            Debug.Log($"Finished downloading or updating Steam Workshop items");
        }
    }

    private async Task DownloadWorkshopItemsAsync(List<Item> workshopItems)
    {
        if (workshopItems.Count(item => !item.IsInstalled) <= 0)
        {
            // All items already downloaded
            return;
        }

        for (int i = 0; i < workshopItems.Count; i++)
        {
            Item workshopItem = workshopItems[i];
            try
            {
                Debug.Log($"Downloading or updating Steam Workshop item {i}/{workshopItems.Count} with id {workshopItem.Id}");
                await workshopItem.DownloadAsync();
                Debug.Log($"Finished downloading or updating Steam Workshop item {i}/{workshopItems.Count} with id {workshopItem.Id}");

                downloadedItems.Add(workshopItem);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to download Steam Workshop item {i}/{workshopItems.Count} with id {workshopItem.Id}: {ex.Message}");
            }
        }
    }

    private async Task<List<Item>> QuerySubscribedWorkshopItemsAsync()
    {
        Debug.Log($"Querying subscribed Steam Workshop items");
        List<Item> result = await ReadAllPages(Query.All.WhereUserSubscribed());
        Debug.Log($"Found {result.Count} subscribed Steam Workshop items");
        return result;
    }

    private async Task<List<Item>> QueryPublishedWorkshopItemsAsync()
    {
        Debug.Log($"Querying published Steam Workshop items");
        List<Item> result = await ReadAllPages(Query.All.WhereUserPublished());
        Debug.Log($"Found {result.Count} published Steam Workshop items");
        return result;
    }

    private async Task<List<Item>> ReadAllPages(Query ugcQuery)
    {
        List<Item> result = new List<Item>();

        // Page number starts at 1
        int page = 1;
        bool hasMorePages;
        do
        {
            ResultPage? resultPage = await ugcQuery
                .GetPageAsync(page);
            if (resultPage != null)
            {
                hasMorePages = resultPage?.ResultCount > 0 && resultPage?.TotalCount > result.Count;
                result.AddRange(resultPage?.Entries);
            }
            else
            {
                hasMorePages = false;
            }
        } while (hasMorePages);

        return result;
    }

    public async Task<PublishResult> PublishNewWorkshopItemAsync(
        string contentFolderPath,
        string previewImagePath,
        string title,
        string description,
        List<string> tags,
        Action<float> onProgress)
    {
        // Check Workshop item titles are unique.
        // Steam allows multiple Workshop Items with the same title.
        // However, this is probably not what the user wanted.
        List<Item> publishedWorkshopItemsAsync = await QueryPublishedWorkshopItemsAsync();
        if (publishedWorkshopItemsAsync.AnyMatch(item => string.Equals(item.Title, title, StringComparison.CurrentCultureIgnoreCase)))
        {
            throw new SteamException($"Steam Workshop item with title '{title}' already exists for this user. "
                                     + $"Choose a different title to upload a new Workshop item or create an update for the existing Workshop item.");
        }

        Debug.Log($"Publishing new Steam Workshop item. Title: '{title}', Content folder: '{contentFolderPath}'");
        Editor ugcEditor = Editor.NewCommunityFile
            .ForAppId(SteamConstants.MelodyManiaSteamAppId)
            .WithPublicVisibility()
            .WithTitle(title)
            .WithDescription(description)
            .WithContent(contentFolderPath)
            .WithPreviewFile(previewImagePath);
        foreach (string tag in tags)
        {
            ugcEditor.WithTag(tag);
        }

        return await ugcEditor
            .SubmitAsync(new ActionProgress(onProgress));
    }

    public enum EDownloadState
    {
        Pending,
        Started,
        Finished
    }
}
