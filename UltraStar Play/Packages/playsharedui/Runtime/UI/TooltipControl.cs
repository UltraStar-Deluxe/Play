using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

public class TooltipControl
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        openTooltipControls.Clear();
    }

    private static readonly HashSet<TooltipControl> openTooltipControls = new();
    public static IReadOnlyCollection<TooltipControl> OpenTooltipControls => openTooltipControls;

    private static readonly float defaultShowDelayInSeconds = 1f;
    private static readonly float defaultCloseDelayInSeconds = 0.2f;
    private static readonly Vector2 tooltipOffsetInPx = new(10, 10);
    private static readonly float showTooltipOnPointerDownTimeInSeconds = 4f;

    public float ShowDelayInSeconds { get; set; } = defaultShowDelayInSeconds;
    public float CloseDelayInSeconds { get; set; } = defaultCloseDelayInSeconds;
    public Translation TooltipText { get; set; }
    public float Margin { get; set; } = 4;

    private readonly VisualElement visualElement;

    private bool isPointerOver;

    private Label label;
    private CancellationTokenSource showTooltipCancellationTokenSource;
    private CancellationTokenSource closeTooltipCancellationTokenSource;
    private bool tooltipVisibleWithAutoClose;

    public bool ShowTooltipOnPointerDown { get; set; } = true;

    public TooltipControl(
        VisualElement visualElement,
        Translation tooltipText = default,
        bool showTooltipOnPointerDown = true)
    {
        this.visualElement = visualElement;
        this.TooltipText = tooltipText;
        this.ShowTooltipOnPointerDown = showTooltipOnPointerDown;

        this.visualElement.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnter(), TrickleDown.TrickleDown);
        this.visualElement.RegisterCallback<PointerLeaveEvent>(evt => OnPointerExit(), TrickleDown.TrickleDown);
        this.visualElement.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(), TrickleDown.TrickleDown);
    }

    private void OnPointerDown()
    {
        if (!Application.isPlaying
            || !ShowTooltipOnPointerDown)
        {
            return;
        }

        ShowTooltipWithAutoClose();
    }

    private UIDocument GetUiDocument()
    {
        return UIDocumentUtils.FindUIDocumentOrThrow();
    }

    private void OnPointerEnter()
    {
        isPointerOver = true;

        if (tooltipVisibleWithAutoClose)
        {
            return;
        }

        ShowTooltipAfterDelayAsync();
    }

    private async void ShowTooltipAfterDelayAsync()
    {
        closeTooltipCancellationTokenSource?.Cancel();

        showTooltipCancellationTokenSource?.Cancel();
        showTooltipCancellationTokenSource = new CancellationTokenSource();

        await Awaitable.WaitForSecondsAsync(ShowDelayInSeconds);
        if (showTooltipCancellationTokenSource.IsCancellationRequested
            || tooltipVisibleWithAutoClose
            || !isPointerOver)
        {
            return;
        }

        ShowTooltip();
    }

    private void OnPointerExit()
    {
        isPointerOver = false;

        if (tooltipVisibleWithAutoClose)
        {
            return;
        }

        CloseTooltipAfterDelayAsync();
    }

    private async void CloseTooltipAfterDelayAsync()
    {
        showTooltipCancellationTokenSource?.Cancel();

        closeTooltipCancellationTokenSource?.Cancel();
        closeTooltipCancellationTokenSource = new CancellationTokenSource();
        await Awaitable.WaitForSecondsAsync(CloseDelayInSeconds);
        if (closeTooltipCancellationTokenSource.IsCancellationRequested
            || tooltipVisibleWithAutoClose
            || isPointerOver)
        {
            return;
        }

        CloseTooltip();
    }

    public void CloseTooltip()
    {
        if (label == null)
        {
            return;
        }
        label.RemoveFromHierarchy();
        label = null;

        openTooltipControls.Remove(this);
    }

    public void ShowTooltip()
    {
        ShowTooltip(GetDefaultTooltipPosition());
    }

    public void ShowTooltip(Vector2 pos)
    {
        CloseTooltip();

        if (TooltipText.Value.IsNullOrEmpty())
        {
            return;
        }

        label = new Label();
        label.name = "tooltipLabel";
        label.AddToClassList("tooltip");
        label.pickingMode = PickingMode.Ignore;
        label.SetTranslatedText(TooltipText);
        label.style.position = new StyleEnum<Position>(Position.Absolute);
        label.style.left = pos.x;
        label.style.top = pos.y;

        GetUiDocument().rootVisualElement.Add(label);

        label.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
            VisualElementUtils.MoveVisualElementFullyInsideScreen(label, GetPanelHelper(), Margin, Margin, Margin, Margin));

        openTooltipControls.Add(this);
    }

    public void ShowTooltipWithAutoClose()
    {
        ShowTooltipWithAutoClose(GetDefaultTooltipPosition());
    }

    public async void ShowTooltipWithAutoClose(Vector2 pos)
    {
        try
        {
            tooltipVisibleWithAutoClose = true;
            ShowTooltip(pos);

            await Awaitable.WaitForSecondsAsync(showTooltipOnPointerDownTimeInSeconds);
            CloseTooltip();
        }
        finally
        {
            tooltipVisibleWithAutoClose = false;
        }
    }

    private Vector2 GetDefaultTooltipPosition()
    {
        return InputUtils.GetPointerPositionInPanelCoordinates(GetPanelHelper(), true) + tooltipOffsetInPx;
    }

    private PanelHelper GetPanelHelper()
    {
        return new PanelHelper(GetUiDocument());
    }
}
