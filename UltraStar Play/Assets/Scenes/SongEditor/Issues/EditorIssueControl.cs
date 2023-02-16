using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorIssueControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    public SongIssue SongIssue { get; private set; }

    [Inject]
    private Injector injector;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    [Inject(UxmlName = R.UxmlNames.issueImage)]
    private VisualElement issueImage;

    private TooltipControl tooltipControl;

    public void OnInjectionFinished()
    {
        if (SongIssue.Severity == ESongIssueSeverity.Warning)
        {
            issueImage.AddToClassList("warning");
        }
        else if (SongIssue.Severity == ESongIssueSeverity.Error)
        {
            issueImage.AddToClassList("error");
        }

        tooltipControl = new TooltipControl(VisualElement);
        tooltipControl.TooltipText = SongIssue.Message;
    }
}
