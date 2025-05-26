using UniRx;
using UnityEngine.UIElements;

public class SongSelectSlideInControlUtils
{
    public static void InitSlideInControl(
        VisualElementSlideInControl slideInControl,
        Button toggleOverlayButton,
        Button closeOverlayButton,
        VisualElement overlay,
        VisualElement hideOverlayArea)
    {
        hideOverlayArea.HideByDisplay();
        hideOverlayArea.RegisterCallback<PointerDownEvent>(_ => slideInControl.SlideOut());

        toggleOverlayButton.RegisterCallbackButtonTriggered(_ => slideInControl.ToggleVisible());
        closeOverlayButton.RegisterCallbackButtonTriggered(_ => slideInControl.SlideOut());

        slideInControl.Visible.Subscribe(newValue =>
        {
            hideOverlayArea.SetVisibleByDisplay(newValue);
            if (newValue)
            {
                closeOverlayButton.Focus();
            }
            else if (VisualElementUtils.IsDescendantFocused(overlay))
            {
                toggleOverlayButton.Focus();
            }
        });
    }

}
