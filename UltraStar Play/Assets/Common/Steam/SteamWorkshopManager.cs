using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Steamworks.Ugc;
using UniInject;
using UniRx;
using UnityEngine;

public class SteamWorkshopManager : AbstractSingletonBehaviour, INeedInjection, IInjectionFinishedListener
{
    public static SteamWorkshopManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SteamWorkshopManager>();

    private readonly UseSteamWorkshopItemsControl useSteamWorkshopItemsControl = new();
    private readonly Subject<bool> finishDownloadWorkshopItemsEventStream = new();
    public IObservable<bool> FinishDownloadWorkshopItemsEventStream => finishDownloadWorkshopItemsEventStream
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
        if (DownloadState is not EDownloadState.Pending)
        {
            return;
        }

        ObservableUtils.RunOnNewTaskAsObservable(async () =>
                await DownloadSubscribedWorkshopItemsAsync())
            .ObserveOnMainThread()
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to download Steam Workshop items: {ex.Message}");
                FireDownloadFinishedEvent();
            })
            .Subscribe(items =>
            {
                Debug.Log($"Successfully downloaded {items.Count} Steam Workshop Items");
                DownloadedWorkshopItems = items;
                FireDownloadFinishedEvent();
                useSteamWorkshopItemsControl.UseWorkshopItems(items);
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
                Debug.Log($"Downloading or updating Steam Workshop item {i + 1}/{items.Count} with id {item.Id}");
                await item.DownloadAsync();
                Debug.Log($"Finished downloading or updating Steam Workshop item {i + 1}/{items.Count} with id {item.Id}. Folder: {item.Directory}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to download Steam Workshop item {i + 1}/{items.Count} with id {item.Id}: {ex.Message}");
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

    public async Task<PublishResult> PublishWorkshopItemAsync(
        string contentFolderPath,
        string previewImagePath,
        string title,
        string description,
        List<string> tags,
        Action<float> onProgress)
    {
        ulong existingWorkshopItemId = GetExistingWorkshopItemIdFromWorkshopItemContentFolder(contentFolderPath);
        bool isNewWorkshopItem = existingWorkshopItemId <= 0;

        if (isNewWorkshopItem)
        {
            if (title.IsNullOrEmpty())
            {
                throw new SteamException("Title cannot be empty");
            }

            // Check Workshop item titles are unique.
            // Steam allows multiple Workshop Items with the same title.
            // However, this is probably not what the user wanted.
            List<Item> publishedWorkshopItemsAsync = await QueryPublishedWorkshopItemsAsync();
            if (publishedWorkshopItemsAsync.AnyMatch(item => string.Equals(item.Title, title, StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new SteamException($"Steam Workshop item with title '{title}' already exists for this user. "
                                         + $"Choose a different title to upload a new Workshop item or create an update for the existing Workshop item.");
            }
        }

        Debug.Log($"Publishing Steam Workshop item. Existing item id: {existingWorkshopItemId}, Title: '{title}', Content folder: '{contentFolderPath}'");
        Editor ugcEditor = isNewWorkshopItem
            ? Editor.NewCommunityFile.WithPublicVisibility()
            : new Editor(existingWorkshopItemId);

        ugcEditor
            .ForAppId(SteamConstants.MelodyManiaSteamAppId)
            .WithContent(contentFolderPath);

        // When updating a workshop item, then only the provided properties are updated.
        // Other properties are unchanged and keep their old value.
        // Thus, only set properties when a value is provided.
        if (!title.IsNullOrEmpty())
        {
            ugcEditor.WithTitle(title);
        }

        if (description.IsNullOrEmpty())
        {
            ugcEditor.WithDescription(description);
        }

        if (!previewImagePath.IsNullOrEmpty())
        {
            if (!FileUtils.Exists(previewImagePath))
            {
                throw new SteamException($"Preview image does not exist: {previewImagePath}");
            }
            ugcEditor.WithPreviewFile(previewImagePath);
        }

        if (!tags.IsNullOrEmpty())
        {
            foreach (string tag in tags)
            {
                ugcEditor.WithTag(tag);
            }
        }

        return await ugcEditor
            .SubmitAsync(new ActionProgress(onProgress));
    }

    private ulong GetExistingWorkshopItemIdFromWorkshopItemContentFolder(string contentFolderPath)
    {
        // Steam Workshop Items are installed in a folder "steamapps\workshop\content\<steam-app-od>\<workshop-item-id>"
        List<DirectoryInfo> directoryInfos = DirectoryUtils.GetParentDirectories(new DirectoryInfo(contentFolderPath), true);
        if (directoryInfos.Count >= 5
            && directoryInfos[4].Name.ToLowerInvariant() == "steamapps"
            && directoryInfos[3].Name.ToLowerInvariant() == "workshop"
            && directoryInfos[2].Name.ToLowerInvariant() == "content"
            && directoryInfos[1].Name == SteamConstants.MelodyManiaSteamAppId.ToString()
            && ulong.TryParse(directoryInfos[0].Name, out ulong workshopItemId))
        {
            return workshopItemId;
        }

        return 0;
    }

    public enum EDownloadState
    {
        Pending,
        Started,
        Finished
    }
}
