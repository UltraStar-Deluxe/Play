
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class MainSceneTabGroupUiControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private GameObject gameObject;

    [Inject]
    private SongListControl songListControl;

    [Inject(UxmlName = R.UxmlNames.showMicViewButton)]
    private Button showMicViewButton;

    [Inject(UxmlName = R.UxmlNames.micViewContainer)]
    private VisualElement micViewContainer;

    [Inject(UxmlName = R.UxmlNames.showSongViewButton)]
    private Button showSongViewButton;

    [Inject(UxmlName = R.UxmlNames.songViewContainer)]
    private VisualElement songViewContainer;

    [Inject(UxmlName = R.UxmlNames.showInputSimulationButton)]
    private Button showInputSimulationButton;

    [Inject(UxmlName = R.UxmlNames.inputSimulationContainer)]
    private VisualElement inputSimulationContainer;

    [Inject]
    private MainGameHttpClient mainGameHttpClient;

    public void OnInjectionFinished()
    {
        TabGroupControl tabGroupControl = new TabGroupControl();
        tabGroupControl.AllowNoContainerVisible = false;
        tabGroupControl.AddTabGroupButton(showMicViewButton, micViewContainer);
        tabGroupControl.AddTabGroupButton(showSongViewButton, songViewContainer);
        tabGroupControl.AddTabGroupButton(showInputSimulationButton, inputSimulationContainer);
        tabGroupControl.ShowContainer(micViewContainer);

        showSongViewButton.RegisterCallbackButtonTriggered(_ => songListControl.Show());

        mainGameHttpClient.Permissions
            .Subscribe(permissions => OnPermissionsChanged(permissions))
            .AddTo(gameObject);
    }

    private void OnPermissionsChanged(List<RestApiPermission> permissions)
    {
        showInputSimulationButton.SetVisibleByDisplay(permissions.Contains(RestApiPermission.WriteInputSimulation));
        inputSimulationContainer.HideByDisplay();
    }

}
