using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.ServerList;
using Steamworks.Ugc;
using UniInject;
using UniRx;
using UnityEngine;

public class SteamWorkshopManager : AbstractSingletonBehaviour, INeedInjection, IInjectionFinishedListener
{
    public static SteamWorkshopManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SteamWorkshopManager>();

    private readonly UseSteamWorkshopItemsControl useSteamWorkshopItemsControl = new();
    private readonly Subject<VoidEvent> finishDownloadWorkshopItemsEventStream = new();
    public IObservable<VoidEvent> FinishDownloadWorkshopItemsEventStream => finishDownloadWorkshopItemsEventStream
        .ObserveOnMainThread();

    [Inject]
    private Injector injector;

    public EDownloadState DownloadState { get; private set; } = EDownloadState.Pending;

    public IReadOnlyList<Item> DownloadedWorkshopItems { get; private set; } = new List<Item>();

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
        DownloadWorkshopItemsAsObservable()
            .Subscribe(_ => Debug.Log("Download workshop items done"));
    }

    public IObservable<IReadOnlyList<Item>> DownloadWorkshopItemsAsObservable()
    {
        return ObservableUtils.RunOnNewTaskAsObservable(async () =>
                await DownloadSubscribedWorkshopItemsAsync())
            .ObserveOnMainThread()
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to download Steam Workshop items: {ex.Message}");
                FireDownloadFinishedEvent();
            })
            .Select(items =>
            {
                Debug.Log($"Successfully downloaded {items.Count} Steam Workshop Items");
                DownloadedWorkshopItems = items;
                FireDownloadFinishedEvent();
                useSteamWorkshopItemsControl.UseWorkshopItems(items);
                return DownloadedWorkshopItems;
            });
    }

    private void FireDownloadFinishedEvent()
    {
        try
        {
            finishDownloadWorkshopItemsEventStream.OnNext(VoidEvent.instance);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to notify subscribers about downloaded workshop items: {ex.Message}");
        }
    }

    private async Task<List<Item>> DownloadSubscribedWorkshopItemsAsync()
    {
        try
        {
            DownloadState = EDownloadState.Started;
            List<Item> workshopItems = await QuerySubscribedWorkshopItemsAsync();
            Debug.Log($"Downloading or updating {workshopItems.Count} Steam Workshop items");
            await DownloadWorkshopItemsAsync(workshopItems);

            return workshopItems;
        }
        finally
        {
            DownloadState = EDownloadState.Finished;
            Debug.Log($"Finished downloading or updating Steam Workshop items");
        }
    }

    private async Task DownloadWorkshopItemsAsync(List<Item> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            try
            {
                Debug.Log($"Downloading or updating Steam Workshop item {i + 1}/{items.Count} '{item.Title}' with id {item.Id}");
                await item.DownloadAsync();
                Debug.Log($"Finished downloading or updating Steam Workshop item {i + 1}/{items.Count} '{item.Title}' with id {item.Id}. Folder: {item.Directory}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to download Steam Workshop item {i + 1}/{items.Count} '{item.Title}' with id {item.Id}: {ex.Message}");
            }
        }
    }

    private async Task<List<Item>> QuerySubscribedWorkshopItemsAsync()
    {
        Debug.Log($"Querying subscribed Steam Workshop items");
        List<Item> result = await ReadAllPages(Query.Items.WhereUserSubscribed());
        Debug.Log($"Found {result.Count} subscribed Steam Workshop items");
        return result;
    }

    private async Task<List<Item>> QueryPublishedWorkshopItemsAsync()
    {
        Debug.Log($"Querying published Steam Workshop items");
        List<Item> result = await ReadAllPages(Query.Items.WhereUserPublished());
        Debug.Log($"Found {result.Count} published Steam Workshop items");
        return result;
    }

    private async Task<List<Item>> ReadAllPages(Query ugcQuery)
    {
        // Some entries are returned multiple times. Thus, Use dictionary with unique ID as key.
        Dictionary<ulong, Item> itemIdToItem = new();

        // Page number starts at 1
        int page = 1;
        bool hasMorePages;
        do
        {
            ResultPage? resultPage = await ugcQuery
                .GetPageAsync(page);
            if (resultPage != null)
            {
                hasMorePages = resultPage?.ResultCount > 0 && resultPage?.TotalCount > itemIdToItem.Count;
                foreach (Item resultPageEntry in resultPage?.Entries)
                {
                    itemIdToItem[resultPageEntry.Id] = resultPageEntry;
                }
            }
            else
            {
                hasMorePages = false;
            }
        } while (hasMorePages);

        return itemIdToItem.Values
            .OrderBy(it => it.Id)
            .ToList();
    }

    public async Task<PublishResult> PublishWorkshopItemAsync(
        ulong workshopItemId,
        string contentFolderPath,
        string previewImagePath,
        string title,
        string description,
        List<string> tags,
        Action<float> onProgress)
    {
        bool isNewWorkshopItem = workshopItemId <= 0;
        if (isNewWorkshopItem)
        {
            string errorMessage = await GetNewWorkshopItemErrorMessage(contentFolderPath, previewImagePath, title);
            if (!errorMessage.IsNullOrEmpty())
            {
                throw new SteamException(errorMessage);
            }
        }

        Debug.Log($"Publishing Steam Workshop item. " +
                  $"Item id: {workshopItemId}, " +
                  $"Title: '{title}', " +
                  $"Content folder: '{contentFolderPath}', " +
                  $"Preview Image: '{previewImagePath}', " +
                  $"Description: '{description}', " +
                  $"Tags: '{tags.JoinWith(", ")}'");

        Editor ugcEditor = isNewWorkshopItem
            ? Editor.NewCommunityFile.WithPublicVisibility()
            : new Editor(workshopItemId);

        ugcEditor
            .ForAppId(SteamConstants.MelodyManiaSteamAppId);

        // When updating a workshop item, then only the provided properties are updated.
        // Other properties are unchanged and keep their old value.
        // Thus, only set properties when a value is provided.
        if (!contentFolderPath.IsNullOrEmpty())
        {
            ugcEditor.WithContent(contentFolderPath);
        }

        if (!previewImagePath.IsNullOrEmpty())
        {
            if (!FileUtils.Exists(previewImagePath))
            {
                throw new SteamException($"Preview image does not exist: {previewImagePath}");
            }
            ugcEditor.WithPreviewFile(previewImagePath);
        }

        if (!title.IsNullOrEmpty())
        {
            ugcEditor.WithTitle(title);
        }

        if (!description.IsNullOrEmpty())
        {
            ugcEditor.WithDescription(description);
        }

        if (!tags.IsNullOrEmpty())
        {
            tags.ForEach(it => ugcEditor.WithTag(it));
        }

        return await ugcEditor
            .SubmitAsync(new ActionProgress(onProgress));
    }

    public async Task SubscribeAndDownloadWorkshopItemAsync(ulong workshopItemFileId)
    {
        ResultPage? resultPage = await Query.Items.WithFileId(workshopItemFileId).GetPageAsync(1);
        if (!resultPage.HasValue)
        {
            return;
        }
        await resultPage.Value.Entries.FirstOrDefault().Subscribe();

        await DownloadWorkshopItemsAsObservable();
    }

    public async Task UnsubscribeAndDownloadWorkshopItemAsync(ulong workshopItemFileId)
    {
        ResultPage? resultPage = await Query.Items.WithFileId(workshopItemFileId).GetPageAsync(1);
        if (!resultPage.HasValue)
        {
            return;
        }
        await resultPage.Value.Entries.FirstOrDefault().Unsubscribe();

        await DownloadWorkshopItemsAsObservable();
    }

    private async Task<string> GetNewWorkshopItemErrorMessage(string contentFolderPath, string previewImagePath, string title)
    {
        if (!DirectoryUtils.Exists(contentFolderPath))
        {
            return "Folder does not exist";
        }

        if (!FileUtils.Exists(previewImagePath))
        {
            return "Preview image does not exist";
        }

        if (new FileInfo(previewImagePath).Length > 1_000_000)
        {
            return "Preview image must be smaller than 1 MB";
        }

        if (title.IsNullOrEmpty())
        {
            return "Title cannot be empty";
        }

        // Check Workshop item titles are unique.
        // Steam allows multiple Workshop Items with the same title.
        // However, this is probably not what the user wanted.
        List<Item> publishedWorkshopItemsAsync = await QueryPublishedWorkshopItemsAsync();
        if (publishedWorkshopItemsAsync.AnyMatch(item => string.Equals(item.Title, title, StringComparison.CurrentCultureIgnoreCase)))
        {
            return $"Steam Workshop item with title '{title}' already exists for this user. " +
                   $"Choose a different title to upload a new Workshop item or create an update for the existing Workshop item.";
        }

        return "";
    }

    public enum EDownloadState
    {
        Pending,
        Started,
        Finished
    }

    public void OpenSteamWorkshopOverlay(string uri)
    {
        SteamFriends.OpenWebOverlay(uri);
    }
}
