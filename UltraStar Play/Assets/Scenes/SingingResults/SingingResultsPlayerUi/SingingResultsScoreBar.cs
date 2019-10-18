using UnityEngine;
using UnityEngine.UI;

public class SingingResultsScoreBar : MonoBehaviour
{
    public double TargetValue { get; set; }

    private double displayedValue;
    private float startHeightPercent;

    private Image image;
    private RectTransform rectTransform;

    void Awake()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        startHeightPercent = rectTransform.anchorMax.y;
    }

    void Update()
    {
        if (TargetValue <= 0)
        {
            return;
        }

        double displayedValuePercent = displayedValue / PlayerScoreController.MaxScore;
        float heightPercent = Mathf.Max(startHeightPercent, (float)displayedValuePercent);
        rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, heightPercent);
        displayedValue = CountingNumberText.GetNextValue(displayedValue, TargetValue);
    }
}
