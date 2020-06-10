using UnityEngine;
using UnityEngine.UI;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingingResultsScoreBar : MonoBehaviour, INeedInjection, IInjectionFinishedListener, IExcludeFromSceneInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private RectTransform rectTransform;

    [Inject]
    private PlayerScoreControllerData playerScoreData;

    private double targetValue;
    private double displayedValue;
    private float startHeightPercent;

    public void OnInjectionFinished()
    {
        targetValue = playerScoreData.NormalNotesTotalScore;
        startHeightPercent = rectTransform.anchorMax.y;
    }

    void Update()
    {
        if (targetValue <= 0
            || startHeightPercent <= 0)
        {
            return;
        }

        double displayedValuePercent = displayedValue / PlayerScoreController.MaxScore;
        float heightPercent = Mathf.Max(startHeightPercent, (float)displayedValuePercent);
        rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, heightPercent);
        displayedValue = CountingNumberText.GetNextValue(displayedValue, targetValue);
    }
}
