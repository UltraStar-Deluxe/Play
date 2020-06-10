using UniInject;
using UnityEngine;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerNameText : MonoBehaviour, INeedInjection, IInjectionFinishedListener, IExcludeFromSceneInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Text text;

    [Inject]
    private PlayerProfile playerProfile;

    [Inject(optional = true)]
    private MicProfile micProfile;

    public void OnInjectionFinished()
    {
        text.text = playerProfile.Name;
        if (micProfile != null)
        {
            text.color = micProfile.Color;
        }
    }
}
