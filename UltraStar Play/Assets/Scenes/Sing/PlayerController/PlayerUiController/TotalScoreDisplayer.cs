using UniInject;
using UnityEngine;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class TotalScoreDisplayer : MonoBehaviour, INeedInjection, IInjectionFinishedListener, IExcludeFromSceneInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private CountingNumberText countingNumberText;

    [Inject(optional = true)]
    private MicProfile micProfile;

    public void ShowTotalScore(int score)
    {
        countingNumberText.TargetValue = score;
    }

    public void OnInjectionFinished()
    {
        if (micProfile != null)
        {
            GetComponentInChildren<Image>().color = micProfile.Color;
        }
    }
}
