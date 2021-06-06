using UnityEngine;
using UniInject;
using UnityEngine.EventSystems;
using ProTrans;

public class TooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [InjectedInInspector]
    [TextArea(3, 10)]
    public string tooltipText;

    [InjectedInInspector]
    public string i18nKey;

    private readonly float defaultShowDelayInSeconds = 1f;
    private readonly float defaultCloseDelayInSeconds = 0.5f;

    [Range(-1, 2)]
    public float showDelayInSeconds = -1f;
    [Range(-1, 2)]
    public float closeDelayInSeconds = -1f;

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

    void Start()
    {
        if (!i18nKey.IsNullOrEmpty()
            && tooltipText.IsNullOrEmpty())
        {
            tooltipText = TranslationManager.GetTranslation(i18nKey);
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
        tooltipShowTime = Time.time + (showDelayInSeconds >= 0 ? showDelayInSeconds : defaultShowDelayInSeconds);
        tooltipCloseTime = -1;
    }

    public void OnPointerExit(PointerEventData ped)
    {
        tooltipShowTime = -1;
        tooltipCloseTime = Time.time + (closeDelayInSeconds >= 0 ? closeDelayInSeconds : defaultCloseDelayInSeconds);
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
