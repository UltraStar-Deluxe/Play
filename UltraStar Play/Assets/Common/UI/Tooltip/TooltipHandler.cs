using UnityEngine;
using UniInject;
using UnityEngine.EventSystems;

public class TooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [InjectedInInspector]
    [TextArea(3, 10)]
    public string tooltipText;

    private readonly float showDelayInSeconds = 1f;
    private readonly float closeDelayInSeconds = 0.5f;

    private float tooltipShowTime = -1;
    private float tooltipCloseTime = -1;

    private Tooltip tooltipInstance;

    private Canvas canvas;
    private Canvas Canvas
    {
        get
        {
            if (canvas == null)
            {
                canvas = CanvasUtils.FindCanvas();
            }
            return canvas;
        }
    }

    void Update()
    {
        if (tooltipShowTime >= 0
            && Time.time >= tooltipShowTime
            && tooltipInstance == null)
        {
            tooltipShowTime = -1;
            ShowTooltip();
        }

        if (tooltipCloseTime >= 0
            && Time.time >= tooltipCloseTime
            && tooltipInstance != null)
        {
            tooltipCloseTime = -1;
            CloseTooltip();
        }
    }

    public void OnPointerEnter(PointerEventData ped)
    {
        tooltipShowTime = Time.time + showDelayInSeconds;
        tooltipCloseTime = -1;
    }

    public void OnPointerExit(PointerEventData ped)
    {
        tooltipShowTime = -1;
        tooltipCloseTime = Time.time + closeDelayInSeconds;
    }

    public void CloseTooltip()
    {
        if (tooltipInstance == null)
        {
            return;
        }
        Destroy(tooltipInstance.gameObject);
        tooltipInstance = null;
    }

    public void ShowTooltip()
    {
        CloseTooltip();

        tooltipInstance = Instantiate(GetTooltipPrefab(), Canvas.transform);
        tooltipInstance.Text = tooltipText;
        tooltipInstance.RectTransform.position = Input.mousePosition;
    }

    private Tooltip GetTooltipPrefab()
    {
        return UiManager.Instance.tooltipPrefab;
    }
}
