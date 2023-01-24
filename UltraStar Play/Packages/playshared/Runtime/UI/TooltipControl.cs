using System.Collections;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class TooltipControl : INeedInjection, IInjectionFinishedListener
{
    private static readonly float defaultShowDelayInSeconds = 1f;
    private static readonly float defaultCloseDelayInSeconds = 0.2f;
    private static readonly Vector2 tooltipOffsetInPx = new(10, 10);
    private static readonly float showTooltipOnPointerDownTimeInSeconds = 4f;

    public float ShowDelayInSeconds { get; set; } = defaultShowDelayInSeconds;
    public float CloseDelayInSeconds { get; set; } = defaultCloseDelayInSeconds;
    public string TooltipText { get; set; }

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement target;

    [Inject]
    private UIDocument uiDocument;

    private Label label;
    private PanelHelper panelHelper;
    private IEnumerator showTooltipCoroutine;
    private IEnumerator closeTooltipCoroutine;
    private bool showTooltipByPointerDown;

    public void OnInjectionFinished()
    {
        this.panelHelper = new PanelHelper(uiDocument);
        target.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnter());
        target.RegisterCallback<PointerLeaveEvent>(evt => OnPointerExit());
        target.RegisterCallback<PointerDownEvent>(evt => OnPointerDown());
    }

    private void OnPointerDown()
    {
        showTooltipByPointerDown = true;
        ShowTooltip();
        uiDocument.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(showTooltipOnPointerDownTimeInSeconds, () =>
        {
            showTooltipByPointerDown = false;
            CloseTooltip();
        }));
    }

    private void OnPointerEnter()
    {
        if (showTooltipByPointerDown)
        {
            return;
        }

        if (closeTooltipCoroutine != null)
        {
            uiDocument.StopCoroutine(closeTooltipCoroutine);
        }

        showTooltipCoroutine = CoroutineUtils.ExecuteAfterDelayInSeconds(ShowDelayInSeconds, () =>
        {
            if (showTooltipByPointerDown)
            {
                return;
            }
            ShowTooltip();
        });
        uiDocument.StartCoroutine(showTooltipCoroutine);
    }

    private void OnPointerExit()
    {
        if (showTooltipByPointerDown)
        {
            return;
        }

        if (showTooltipCoroutine != null)
        {
            uiDocument.StopCoroutine(showTooltipCoroutine);
        }

        closeTooltipCoroutine = CoroutineUtils.ExecuteAfterDelayInSeconds(CloseDelayInSeconds, () => CloseTooltip());
        uiDocument.StartCoroutine(closeTooltipCoroutine);
    }

    public void CloseTooltip()
    {
        if (label == null)
        {
            return;
        }
        label.RemoveFromHierarchy();
    }

    public void ShowTooltip()
    {
        CloseTooltip();

        if (TooltipText.IsNullOrEmpty())
        {
            return;
        }

        Vector2 pos = InputUtils.GetPointerPositionInPanelCoordinates(panelHelper, true) + tooltipOffsetInPx;

        label = new Label();
        label.AddToClassList("tooltip");
        label.pickingMode = PickingMode.Ignore;
        label.text = TooltipText;
        label.style.position = new StyleEnum<Position>(Position.Absolute);
        label.style.left = pos.x;
        label.style.top = pos.y;

        uiDocument.rootVisualElement.Children().First().Add(label);

        label.RegisterCallbackOneShot<GeometryChangedEvent>(evt => VisualElementUtils.MoveVisualElementFullyInsideScreen(label, panelHelper));
    }
}
