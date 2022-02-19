using System.Collections;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class TooltipControl : INeedInjection, IInjectionFinishedListener
{
    private static readonly float defaultShowDelayInSeconds = 1f;
    private static readonly float defaultCloseDelayInSeconds = 0.2f;

    public float ShowDelayInSeconds { get; set; } = defaultShowDelayInSeconds;
    public float CloseDelayInSeconds { get; set; } = defaultCloseDelayInSeconds;
    public string TooltipText { get; set; }

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement target;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private UiManager uiManager;

    private Label label;
    private PanelHelper panelHelper;
    private IEnumerator showTooltipCoroutine;
    private IEnumerator closeTooltipCoroutine;

    public void OnInjectionFinished()
    {
        this.panelHelper = new PanelHelper(uiDocument);
        target.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnter());
        target.RegisterCallback<PointerLeaveEvent>(evt => OnPointerExit());
    }

    private void OnPointerEnter()
    {
        if (closeTooltipCoroutine != null)
        {
            uiManager.StopCoroutine(closeTooltipCoroutine);
        }

        showTooltipCoroutine = CoroutineUtils.ExecuteAfterDelayInSeconds(ShowDelayInSeconds, () => ShowTooltip());
        uiManager.StartCoroutine(showTooltipCoroutine);
    }

    private void OnPointerExit()
    {
        if (showTooltipCoroutine != null)
        {
            uiManager.StopCoroutine(showTooltipCoroutine);
        }

        closeTooltipCoroutine = CoroutineUtils.ExecuteAfterDelayInSeconds(CloseDelayInSeconds, () => CloseTooltip());
        uiManager.StartCoroutine(closeTooltipCoroutine);
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

        Vector2 pos = InputUtils.GetPointerPositionInPanelCoordinates(panelHelper);

        label = new Label();
        label.AddToClassList("tooltip");
        label.pickingMode = PickingMode.Ignore;
        label.text = TooltipText;
        label.style.position = new StyleEnum<Position>(Position.Absolute);
        label.style.left = pos.x;
        label.style.bottom = pos.y;

        uiDocument.rootVisualElement.Children().First().Add(label);

        label.RegisterCallbackOneShot<GeometryChangedEvent>(evt => VisualElementUtils.MoveVisualElementFullyInsideScreen(label, panelHelper));
    }
}
