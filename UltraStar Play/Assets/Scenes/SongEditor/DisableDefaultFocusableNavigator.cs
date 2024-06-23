// Disable warning about fields that are never assigned, their values are injected.

using UnityEngine;

#pragma warning disable CS0649

public class DisableDefaultFocusableNavigator : MonoBehaviour
{
    private void Awake()
    {
        // Disable default FocusableNavigator
        FindObjectsOfType<DefaultFocusableNavigator>()
            .ForEach(it => it.gameObject.SetActive(false));
    }
    
    private void OnDestroy()
    {
        // Enable default FocusableNavigator
        FindObjectsOfType<DefaultFocusableNavigator>(true)
            .ForEach(it => it.gameObject.SetActive(true));
    }
}
