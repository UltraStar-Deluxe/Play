using UniInject;
using UnityEngine;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class TotalScoreDisplayer : MonoBehaviour, INeedInjection, IExcludeFromSceneInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private CountingNumberText countingNumberText;

    public void ShowTotalScore(int score)
    {
        countingNumberText.TargetValue = score;
    }

    public void SetColor(Color color)
    {
        GetComponentInChildren<ImageHueHelper>().SetHueByColor(color);
    }
}
