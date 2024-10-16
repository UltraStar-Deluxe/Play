using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks.Ugc;
using UniInject;
using UnityEngine;

public class UseSteamWorkshopItemsControl : INeedInjection
{
    [Inject]
    private SteamWorkshopManager steamWorkshopManager;

    public void UseWorkshopItems(List<Item> items)
    {
        if (items.IsNullOrEmpty())
        {
            return;
        }

        Debug.Log($"Using content from {items.Count} Steam Workshop items: {items.Select(it => it.Title).JoinWith(", ")}");
        UseDownloadedWorkshopItemsForPlayerProfileImages(items);
        UseDownloadedWorkshopItemsForMods(items);
        UseDownloadedWorkshopItemsForThemes(items);
    }

    private void UseDownloadedWorkshopItemsForThemes(List<Item> items)
    {
        try
        {
            ThemeFolderUtils.AdditionalThemeFolders = UnionWithSubfolderInWorkshopItems(
                ThemeFolderUtils.AdditionalThemeFolders,
                items,
                ThemeFolderUtils.ThemeFolderName);
            Debug.Log($"Using Steam Workshop items for additional theme folders: {ThemeFolderUtils.AdditionalThemeFolders.JoinWith(", ")}");
            ThemeManager.Instance.ReloadThemes();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to make use of downloaded Workshop Items for themes: {ex.Message}");
        }
    }

    private void UseDownloadedWorkshopItemsForMods(List<Item> items)
    {
        try
        {
            ModFolderUtils.AdditionalModRootFolders = UnionWithSubfolderInWorkshopItems(
                ModFolderUtils.AdditionalModRootFolders,
                items,
                ModFolderUtils.ModsRootFolderName);
            Debug.Log($"Using Steam Workshop items for additional mod folders: {ModFolderUtils.AdditionalModRootFolders.JoinWith(", ")}");
            ModManager.Instance.ReloadMods();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to make use of downloaded Workshop Items for mods: {ex.Message}");
        }
    }

    private void UseDownloadedWorkshopItemsForPlayerProfileImages(List<Item> items)
    {
        try
        {
            PlayerProfileUtils.AdditionalPlayerProfileImageFolders = UnionWithSubfolderInWorkshopItems(
                PlayerProfileUtils.AdditionalPlayerProfileImageFolders,
                items,
                PlayerProfileUtils.PlayerProfileImagesFolderName);
            Debug.Log($"Using Steam Workshop items for additional player profile image folders: {PlayerProfileUtils.AdditionalPlayerProfileImageFolders.JoinWith(", ")}");
            UiManager.Instance.UpdatePlayerProfileImagePaths();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to make use of downloaded Workshop Items for player profile images: {ex.Message}");
        }
    }

    private static List<string> UnionWithSubfolderInWorkshopItems(List<string> originalFolders, List<Item> items, string subfolderName)
    {
        return originalFolders
            .Union(GetFolderPathInWorkshopItems(items, subfolderName))
            .Distinct()
            .ToList();
    }

    private static List<string> GetFolderPathInWorkshopItems(List<Item> items, string folderName)
    {
        return items
            .Select(item => $"{item.Directory}/{folderName}")
            .Distinct()
            .Where(folder => DirectoryUtils.Exists(folder))
            .ToList();
    }
}
