using UnityEngine;
using UniInject;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

/**
 * Disables the EventSystem and InputSystemUiInputModule in the scene on Android
 * if the user did not opt-in for its usage.
 * This is required because of a Unity issue that can make the UI unusable on Android
 * (see https://issuetracker.unity3d.com/issues/android-uitoolkit-buttons-cant-be-clicked-with-a-cursor-in-samsung-dex-when-using-eventsystem)
 */
public class EventSystemOptInOnAndroid : MonoBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        loggedMessage = false;
    }
    private static bool loggedMessage;

    [Inject]
    private Settings settings;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren, Optional = true)]
    private EventSystem eventSystem;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren, Optional = true)]
    private InputSystemUIInputModule inputSystemUiInputModule;

    private void Start()
    {
        if (!PlatformUtils.IsAndroid
            || settings.DeveloperSettings.enableEventSystemOnAndroid)
        {
            return;
        }

        if (!loggedMessage)
        {
            loggedMessage = true;
            Debug.Log($"Disable custom event system on Android.");
        }

        if (eventSystem != null)
        {
            eventSystem.enabled = false;
            eventSystem.gameObject.SetActive(false);
        }

        if (inputSystemUiInputModule != null)
        {
            inputSystemUiInputModule.enabled = false;
            inputSystemUiInputModule.gameObject.SetActive(false);
        }
    }
}
