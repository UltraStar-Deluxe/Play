using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class CursorHandler : MonoBehaviour
{
    // The texture must have type "Cursor" in Unity 3D import settings. The size should be 32x32 pixel.
    public Texture2D cursorTexture;

    // hotSpot is the pixel coordinate in the texture where the actual cursor is measured.
    // Upper-left corner in the texture is coordinate (0,0).
    public Vector2 hotSpot = Vector2.zero;

    void Start()
    {
        SettingsManager.Instance.Settings.GraphicSettings
            .ObserveEveryValueChanged(it => it.useImageAsCursor).Subscribe(newValue => UpdateCursor(newValue));
    }

    void UpdateCursor(bool useImageAsCursor)
    {
        if (useImageAsCursor)
        {
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
