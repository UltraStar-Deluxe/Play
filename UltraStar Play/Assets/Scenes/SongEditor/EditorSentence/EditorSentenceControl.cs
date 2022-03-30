using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorSentenceControl : INeedInjection, IInjectionFinishedListener
{
    private static readonly double handleWidthInPercent = 0.25;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    [Inject(UxmlName = R.UxmlNames.sentenceImage)]
    private VisualElement sentenceImage;

    [Inject(UxmlName = R.UxmlNames.rightHandle)]
    private VisualElement rightHandle;

    [Inject(UxmlName = R.UxmlNames.leftHandle)]
    private VisualElement leftHandle;

    [Inject(UxmlName = R.UxmlNames.selectionIndicator)]
    private VisualElement selectionIndicator;

    [Inject]
    public Sentence Sentence { get; private set; }

    [Inject(UxmlName = R.UxmlNames.sentenceLabel)]
    private Label sentenceLabel;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private CursorManager cursorManager;

    [Inject]
    private Injector injector;

    [Inject(UxmlName = R.UxmlNames.noteArea)]
    private VisualElement noteArea;

    private EditorSentenceContextMenuControl contextMenuControl;
    private ManipulateSentenceDragListener dragListener;

    public bool IsPointerOver { get; private set; }
    public bool IsPointerOverRightHandle { get; private set; }

    public void OnInjectionFinished()
    {
        rightHandle.HideByDisplay();
        leftHandle.HideByDisplay();
        selectionIndicator.HideByDisplay();

        VisualElement.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnter());
        VisualElement.RegisterCallback<PointerLeaveEvent>(evt => OnPointerExit());
        VisualElement.RegisterCallback<PointerMoveEvent>(evt => OnPointerMove(evt));

        if (Sentence.Voice != null)
        {
            Color color = songEditorSceneControl.GetColorForVoice(Sentence.Voice);
            SetColor(color);
        }

        contextMenuControl = injector
            .WithRootVisualElement(VisualElement)
            .WithBindingForInstance(this)
            .CreateAndInject<EditorSentenceContextMenuControl>();

        dragListener = injector
            .WithRootVisualElement(VisualElement)
            .WithBindingForInstance(this)
            .WithBindingForInstance(contextMenuControl)
            .CreateAndInject<ManipulateSentenceDragListener>();
    }

    public void SetColor(Color color)
    {
        sentenceImage.style.backgroundColor = color;
    }

    public void SetText(string label)
    {
        sentenceLabel.text = label;
    }

    private void OnPointerMove(IPointerEvent evt)
    {
        Vector2 localPoint = evt.localPosition;
        float width = VisualElement.worldBound.width;
        double xPercent = localPoint.x / width;
        if (xPercent > (1 - handleWidthInPercent))
        {
            OnPointerOverRightHandle();
        }
        else
        {
            OnPointerOverCenter();
        }

        UpdateHandles();
    }

    private void OnPointerOverCenter()
    {
        IsPointerOverRightHandle = false;
        cursorManager.SetCursor(ECursor.Grab);
    }

    private void OnPointerOverRightHandle()
    {
        IsPointerOverRightHandle = true;
        cursorManager.SetCursor(ECursor.ArrowsLeftRight);
    }

    private void UpdateHandles()
    {
        bool isRightHandleVisible = IsPointerOverRightHandle;
        rightHandle.SetVisibleByDisplay(isRightHandleVisible);
    }

    private void OnPointerEnter()
    {
        IsPointerOver = true;
    }

    private void OnPointerExit()
    {
        IsPointerOver = false;
        IsPointerOverRightHandle = false;
        UpdateHandles();
        cursorManager.SetDefaultCursor();
    }

    public bool IsPositionOverRightHandle(Vector2 screenPosition)
    {
        Vector2 localPoint = screenPosition - VisualElement.worldBound.position;
        float width = VisualElement.worldBound.width;
        double xPercent = localPoint.x / width;
        return xPercent > (1 - handleWidthInPercent);
    }

    public void Dispose()
    {
        dragListener.Dispose();
        VisualElement.RemoveFromHierarchy();
    }
}
