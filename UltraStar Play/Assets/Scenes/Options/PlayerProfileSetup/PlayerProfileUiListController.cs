using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class PlayerProfileUiListController : MonoBehaviour
{
    public Button addButton;
    public Transform scrollViewContent;
    public PlayerProfileUiListEntry listEntryPrefab;

    void Start()
    {
        UpdateListEntries();
        addButton.OnClickAsObservable().Subscribe(_ => CreateNewPlayerProfile());
    }

    private void UpdateListEntries()
    {
        // Remove old list entries
        foreach (Transform child in scrollViewContent.transform)
        {
            Destroy(child.gameObject);
        }

        // Create new list entries
        List<PlayerProfile> playerProfiles = new List<PlayerProfile>(SettingsManager.Instance.Settings.PlayerProfiles);
        foreach (PlayerProfile playerProfile in playerProfiles)
        {
            CreateListEntry(playerProfile);
        }
    }

    private void CreateListEntry(PlayerProfile playerProfile)
    {
        PlayerProfileUiListEntry uiListEntry = Instantiate(listEntryPrefab);
        uiListEntry.transform.SetParent(scrollViewContent);

        uiListEntry.SetPlayerProfile(playerProfile);

        uiListEntry.deleteButton.OnClickAsObservable().Subscribe(_ => DeleteListEntry(playerProfile));
    }

    private void DeleteListEntry(PlayerProfile playerProfile)
    {
        SettingsManager.Instance.Settings.PlayerProfiles.Remove(playerProfile);
        UpdateListEntries();
    }

    private void CreateNewPlayerProfile()
    {
        SettingsManager.Instance.Settings.PlayerProfiles.Add(new PlayerProfile());
        UpdateListEntries();
    }
}
