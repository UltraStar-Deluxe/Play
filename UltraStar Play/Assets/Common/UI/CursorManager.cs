using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniInject;
using UniRx;

#pragma warning disable CS0649

public class CursorManager : MonoBehaviour, INeedInjection
{
    public static CursorManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<CursorManager>("CursorManager");
        }
    }

    [Inject]
    private SettingsManager settingsManager;

    // The texture must have type "Cursor" in Unity 3D import settings. The size should be 32x32 pixel.
    public Texture2D cursorTexture;
    public Texture2D horizontalCursorTexture;
    public Texture2D verticalCursorTexture;
    public Texture2D grabCursorTexture;

    // hotSpot is the pixel coordinate in the texture where the actual cursor is measured.
    // Upper-left corner in the texture is coordinate (0,0).
    public Vector2 defaultHotSpot = Vector2.zero;

    void Start()
    {
        settingsManager.Settings.GraphicSettings
            .ObserveEveryValueChanged(it => it.useImageAsCursor).Subscribe(newValue => SetDefaultCursor());
    }

    public void SetDefaultCursor()
    {
        if (UseImageAsCursor())
        {
            Cursor.SetCursor(cursorTexture, defaultHotSpot, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    public void SetCursorHorizontal()
    {
        if (!UseImageAsCursor())
        {
            return;
        }

        Cursor.SetCursor(horizontalCursorTexture, new Vector2(32, 32), CursorMode.Auto);
    }

    public void SetCursorVertical()
    {
        if (!UseImageAsCursor())
        {
            return;
        }

        Cursor.SetCursor(verticalCursorTexture, new Vector2(32, 32), CursorMode.Auto);
    }

    public void SetCursorGrab()
    {
        if (!UseImageAsCursor())
        {
            return;
        }

        Cursor.SetCursor(grabCursorTexture, new Vector2(32, 32), CursorMode.Auto);
    }

    private bool UseImageAsCursor()
    {
        return settingsManager.Settings.GraphicSettings.useImageAsCursor;
    }
}
