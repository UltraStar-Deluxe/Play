using System.Collections.Generic;
using ProTrans;
using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NewVersionAvailableDialogControl : AbstractModalDialogControl, IInjectionFinishedListener
{
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

    private readonly VisualElement parentVisualElement;

    public NewVersionAvailableDialogControl(VisualElement dialogRootVisualElement,
        VisualElement parentVisualElement,
        PropertiesFile remoteVersionProperties)
    {
        this.DialogRootVisualElement = dialogRootVisualElement;
        this.parentVisualElement = parentVisualElement;
        remoteVersionProperties.TryGetValue("release", out remoteRelease);
        remoteVersionProperties.TryGetValue("name", out releaseName);
        remoteVersionProperties.TryGetValue("website_link", out websiteLink);
    }

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();

        ignoreThisVersionButton.RegisterCallbackButtonTriggered(_ =>
        {
            settings.IgnoredReleases.AddIfNotContains(remoteRelease);
            CloseDialog();
        });

        ignoreAllFutureVersionsButton.RegisterCallbackButtonTriggered(_ =>
        {
            settings.IgnoredReleases.Clear();
            settings.IgnoredReleases.Add("all");
            CloseDialog();
        });

        closeButton.RegisterCallbackButtonTriggered(_ =>
        {
            CloseDialog();
        });

        UpdateTranslation();

        parentVisualElement.Add(DialogRootVisualElement);

        closeButton.Focus();
    }

    public override void CloseDialog()
    {
        base.CloseDialog();
        disposables.ForEach(it => it.Dispose());
    }

    public void UpdateTranslation()
    {
        string displayName = releaseName.IsNullOrEmpty()
            ? remoteRelease.NullToEmpty()
            : releaseName.NullToEmpty();
        dialogMessage.SetTranslatedText(Translation.Get(R.Messages.mainScene_newVersionDialog_message,
            "remoteRelease", displayName,
             "websiteLink", websiteLink.NullToEmpty()));
    }
}
