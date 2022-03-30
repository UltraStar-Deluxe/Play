using System.Linq;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorFocusableNavigator : FocusableNavigator
{
    private void Awake()
    {
        // Disable default FocusableNavigator
        FindObjectsOfType<FocusableNavigator>()
            .Where(it => it != this)
            .ForEach(it => it.gameObject.SetActive(false));
    }
}
