using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class OnlyVisibleWhenControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Settings settings;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private ClientSideCompanionClientManager clientSideCompanionClientManager;

    [Inject(UxmlClass = R.UssClasses.onlyVisibleWhenConnected)]
    private List<VisualElement> onlyVisibleWhenConnected;

    [Inject(UxmlClass = R.UssClasses.onlyVisibleWhenNotConnected)]
    private List<VisualElement> onlyVisibleWhenNotConnected;

    [Inject(UxmlClass = R.UssClasses.onlyVisibleWhenDevModeEnabled)]
    private List<VisualElement> onlyVisibleWhenDevModeEnabled;

    public void OnInjectionFinished()
    {
        // Only show controls when (not) connected
        onlyVisibleWhenConnected.ForEach(it => it.HideByDisplay());
        onlyVisibleWhenNotConnected.ForEach(it => it.ShowByDisplay());

        clientSideCompanionClientManager.ConnectEventStream
            .Subscribe(UpdateConnectionStatus)
            .AddTo(gameObject);

        // Only controls when dev mode is enabled
        UpdateDevModeControlsVisibility();
        settings.ObserveEveryValueChanged(it => it.IsDevModeEnabled)
            .Subscribe(_ => UpdateDevModeControlsVisibility())
            .AddTo(gameObject);
    }

    private void UpdateDevModeControlsVisibility()
    {
        onlyVisibleWhenDevModeEnabled.ForEach(it => it.SetVisibleByDisplay(settings.IsDevModeEnabled));
    }

    private void UpdateConnectionStatus(ConnectEvent connectEvent)
    {
        if (connectEvent.IsSuccess)
        {
            onlyVisibleWhenConnected.ForEach(it => it.ShowByDisplay());
            onlyVisibleWhenNotConnected.ForEach(it => it.HideByDisplay());
        }
        else
        {
            onlyVisibleWhenConnected.ForEach(it => it.HideByDisplay());
            onlyVisibleWhenNotConnected.ForEach(it => it.ShowByDisplay());
        }
    }
}
