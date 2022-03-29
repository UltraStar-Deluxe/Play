using UniInject;
using UnityEngine;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerMessageDisplayer : MonoBehaviour, INeedInjection
{
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Text uiText;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void ShowMessage(string text, Color color, float showMessageDurationInSeconds)
    {
        gameObject.SetActive(true);
        uiText.text = text;
        uiText.color = color;
        
        float hideMessageTime = Time.time + showMessageDurationInSeconds;
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(showMessageDurationInSeconds,
            () => HideMessage(hideMessageTime)));
    }

    private void HideMessage(float hideMessageTime)
    {
        if (Time.time >= hideMessageTime)
        {
            gameObject.SetActive(false);
        }
    }
}
