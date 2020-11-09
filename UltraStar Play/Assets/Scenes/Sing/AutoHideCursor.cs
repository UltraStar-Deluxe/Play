using UnityEngine;

/**
 * Hides the cursor when the mouse is not moved for a while.
 */
public class AutoHideCursor : MonoBehaviour
{
    private static readonly float defaultHideDelayInSeconds = 5f;

    public float initialHideDelayInSeconds = defaultHideDelayInSeconds;

    private float hideDelayInSeconds;

    private Vector3 lastMousePosition;

    private void Awake()
    {
        lastMousePosition = Input.mousePosition;
        hideDelayInSeconds = initialHideDelayInSeconds;
    }

    private void Update()
    {
        if (hideDelayInSeconds <= 0)
        {
            Cursor.visible = false;
        }
        else
        {
            hideDelayInSeconds -= Time.deltaTime;
        }
        if (lastMousePosition != Input.mousePosition
            || Input.anyKeyDown)
        {
            lastMousePosition = Input.mousePosition;

            Cursor.visible = true;
            hideDelayInSeconds = defaultHideDelayInSeconds;
        }
    }

    private void OnDestroy()
    {
        Cursor.visible = true;
    }
}
