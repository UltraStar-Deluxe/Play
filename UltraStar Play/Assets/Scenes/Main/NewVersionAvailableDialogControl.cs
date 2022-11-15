using System;
using System.Collections.Generic;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UnityEngine.UIElements;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NewVersionAvailableDialogControl : INeedInjection, IInjectionFinishedListener, ITranslator
{
    private const int CloseNewVersionAvailableDialogInputActionPriority = 10;

    [Inject(UxmlName = R.UxmlNames.dialogTitle)]
    private Label dialogTitle;

    [Inject(UxmlName = R.UxmlNames.dialogMessage)]
    private Label dialogMessage;

    [Inject(UxmlName = R.UxmlNames.dialogCloseButton)]
    private Button closeButton;

    [Inject(UxmlName = R.UxmlNames.ignoreThisVersionButton)]
    private Button ignoreThisVersionButton;

    [Inject(UxmlName = R.UxmlNames.ignoreAllFutureVersionsButton)]
    private Button ignoreAllFutureVersionsButton;

    [Inject]
    private Settings settings;

    private readonly string remoteRelease;
    private readonly string websiteLink;
    private readonly string releaseName;

    private readonly VisualElement dialogRootVisualElement;
    private readonly VisualElement parentVisualElement;

    private readonly List<IDisposable> disposables = new();

    public NewVersionAvailableDialogControl(VisualElement dialogRootVisualElement,
        VisualElement parentVisualElement,
        Dictionary<string, string> remoteVersionProperties)
    {
        this.dialogRootVisualElement = dialogRootVisualElement;
        this.parentVisualElement = parentVisualElement;
        remoteVersionProperties.TryGetValue("release", out remoteRelease);
        remoteVersionProperties.TryGetValue("name", out releaseName);
        remoteVersionProperties.TryGetValue("website_link", out websiteLink);
    }

    public void OnInjectionFinished()
    {
        // Add callbacks to buttons
        ignoreThisVersionButton.RegisterCallbackButtonTriggered(() =>
        {
            settings.IgnoredReleases.AddIfNotContains(remoteRelease);
            CloseDialog();
        });

        ignoreAllFutureVersionsButton.RegisterCallbackButtonTriggered(() =>
        {
            settings.IgnoredReleases.Clear();
            settings.IgnoredReleases.Add("all");
            CloseDialog();
        });

        closeButton.RegisterCallbackButtonTriggered(() =>
        {
            CloseDialog();
        });

        UpdateTranslation();

        parentVisualElement.Add(dialogRootVisualElement);

        closeButton.Focus();

        disposables.Add(InputManager.GetInputAction(R.InputActions.usplay_back)
            .PerformedAsObservable(CloseNewVersionAvailableDialogInputActionPriority)
            .Subscribe(_ =>
            {
                InputManager.GetInputAction(R.InputActions.usplay_back).CancelNotifyForThisFrame();
                CloseDialog();
            }));
    }

    public void CloseDialog()
    {
        parentVisualElement.Remove(dialogRootVisualElement);
        disposables.ForEach(it => it.Dispose());
    }

    public void UpdateTranslation()
    {
        string displayName = releaseName.IsNullOrEmpty()
            ? remoteRelease.NullToEmpty()
            : releaseName.NullToEmpty();
        dialogMessage.text = TranslationManager.GetTranslation(R.Messages.newVersionAvailableDialog_message, "remoteRelease", displayName, "websiteLink", websiteLink.NullToEmpty());
        dialogTitle.text = TranslationManager.GetTranslation(R.Messages.newVersionAvailableDialog_title);
        ignoreThisVersionButton.text = TranslationManager.GetTranslation(R.Messages.newVersionAvailableDialog_ignoreThisVersion);
        ignoreAllFutureVersionsButton.text = TranslationManager.GetTranslation(R.Messages.newVersionAvailableDialog_ignoreAllFutureVersions);
    }
}
