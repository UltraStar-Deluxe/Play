using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class TooltipControl
{
    private static readonly float defaultShowDelayInSeconds = 1f;
    private static readonly float defaultCloseDelayInSeconds = 0.2f;
    private static readonly Vector2 tooltipOffsetInPx = new(10, 10);
    private static readonly float showTooltipOnPointerDownTimeInSeconds = 4f;

    public float ShowDelayInSeconds { get; set; } = defaultShowDelayInSeconds;
    public float CloseDelayInSeconds { get; set; } = defaultCloseDelayInSeconds;
    public string TooltipText { get; set; }

    private readonly VisualElement visualElement;
    
    private Label label;
    private IEnumerator showTooltipCoroutine;
    private IEnumerator closeTooltipCoroutine;
    private bool showTooltipByPointerDown;

    public TooltipControl(VisualElement visualElement)
    {
        this.visualElement = visualElement;
        
        this.visualElement.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnter());
        this.visualElement.RegisterCallback<PointerLeaveEvent>(evt => OnPointerExit());
        this.visualElement.RegisterCallback<PointerDownEvent>(evt => OnPointerDown());
    }

    private void OnPointerDown()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        
        showTooltipByPointerDown = true;
        ShowTooltip();
        GetUiDocument().StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(showTooltipOnPointerDownTimeInSeconds, () =>
        {
            showTooltipByPointerDown = false;
            CloseTooltip();
        }));
    }

    private UIDocument GetUiDocument()
    {
        return UIDocumentUtils.FindUIDocumentOrThrow();
    }

    private void OnPointerEnter()
    {
        if (showTooltipByPointerDown)
        {
            return;
        }

        if (closeTooltipCoroutine != null)
        {
            GetUiDocument().StopCoroutine(closeTooltipCoroutine);
        }

        showTooltipCoroutine = CoroutineUtils.ExecuteAfterDelayInSeconds(ShowDelayInSeconds, () =>
        {
            if (showTooltipByPointerDown)
            {
                return;
            }
            ShowTooltip();
        });
        GetUiDocument().StartCoroutine(showTooltipCoroutine);
    }

    private void OnPointerExit()
    {
        if (showTooltipByPointerDown)
        {
            return;
        }

        if (showTooltipCoroutine != null)
        {
            GetUiDocument().StopCoroutine(showTooltipCoroutine);
        }

        closeTooltipCoroutine = CoroutineUtils.ExecuteAfterDelayInSeconds(CloseDelayInSeconds, () => CloseTooltip());
        GetUiDocument().StartCoroutine(closeTooltipCoroutine);
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
        
        Vector2 pos = InputUtils.GetPointerPositionInPanelCoordinates(GetPanelHelper(), true) + tooltipOffsetInPx;

        label = new Label();
        label.AddToClassList("tooltip");
        label.pickingMode = PickingMode.Ignore;
        label.text = TooltipText;
        label.style.position = new StyleEnum<Position>(Position.Absolute);
        label.style.left = pos.x;
        label.style.top = pos.y;

        GetUiDocument().rootVisualElement.Children().First().Add(label);

        label.RegisterCallbackOneShot<GeometryChangedEvent>(evt => VisualElementUtils.MoveVisualElementFullyInsideScreen(label, GetPanelHelper()));
    }

    private PanelHelper GetPanelHelper()
    {
        return new PanelHelper(GetUiDocument());
    }
}
