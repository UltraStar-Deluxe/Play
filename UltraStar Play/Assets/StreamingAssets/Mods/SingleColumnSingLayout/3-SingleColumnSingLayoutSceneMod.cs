using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class SingleColumnSingLayoutSceneMod : ISceneMod
{
    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private SingleColumnSingLayoutModSettings modSettings;

    public void OnSceneEntered(SceneEnteredContext sceneEnteredContext)
    {
        if (sceneEnteredContext.Scene != EScene.SingScene)
        {
            return;
        }

        // Check if 4 players
        int playerCount = SceneNavigator.GetSceneDataOrThrow<SingSceneData>().SingScenePlayerData.SelectedPlayerProfiles.Count;
        if (playerCount != 4)
        {
            Debug.Log($"{nameof(SingleColumnSingLayoutSceneMod)} - Not applying single column sing layout. playerCount: {playerCount}");
            return;
        }
        Debug.Log($"{nameof(SingleColumnSingLayoutSceneMod)} - Applying single column sing layout");

        // Must wait a frame for the UI to be fully initialized
        AwaitableUtils.ExecuteAfterDelayInFramesAsync(1, () =>
        {
            ApplySingleColumnLayout();
        });
    }

    private void ApplySingleColumnLayout()
    {
        // Make container a column
        VisualElement playerUiContainer = uiDocument.rootVisualElement.Q("playerUiContainer");
        playerUiContainer.style.flexDirection = FlexDirection.Column;

        List<VisualElement> playerUiRoots = uiDocument.rootVisualElement.Query("playerUiRoot").ToList();
        // Make direct children of container
        playerUiRoots.ForEach(playerUiRoot =>
        {
            playerUiContainer.Add(playerUiRoot);
        });

        // Remove non-player UI from container. These were the containers for the previous layout.
        playerUiContainer.Children()
            .Where(child => !playerUiRoots.Contains(child))
            .ToList()
            .ForEach(nonPlayerUiElement => playerUiContainer.Remove(nonPlayerUiElement));
    }
}
