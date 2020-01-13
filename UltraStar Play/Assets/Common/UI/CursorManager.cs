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

    private static readonly int cursorWidth = 64;
    // hotSpot is the pixel coordinate in the texture where the actual cursor is measured.
    // Upper-left corner in the texture is coordinate (0,0).
    private static readonly Vector2 cursorTopLeftCorner = Vector2.zero;
    private static readonly Vector2 cursorCenter = new Vector2(cursorWidth / 2f, cursorWidth / 2f);

    [Inject]
    private SettingsManager settingsManager;

    // The texture must have type "Cursor" in Unity 3D import settings.
    public Texture2D cursorTexture;
    public Texture2D horizontalCursorTexture;
    public Texture2D verticalCursorTexture;
    public Texture2D grabCursorTexture;

    void Start()
    {
        settingsManager.Settings.GraphicSettings
            .ObserveEveryValueChanged(it => it.useImageAsCursor).Subscribe(newValue => SetDefaultCursor());
        SetDefaultCursor();
    }

    public void SetDefaultCursor()
    {
        if (UseImageAsCursor())
        {
            Cursor.SetCursor(cursorTexture, cursorTopLeftCorner, CursorMode.Auto);
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

        Cursor.SetCursor(horizontalCursorTexture, cursorCenter, CursorMode.Auto);
    }

    public void SetCursorVertical()
    {
        if (!UseImageAsCursor())
        {
            return;
        }

        Cursor.SetCursor(verticalCursorTexture, cursorCenter, CursorMode.Auto);
    }

    public void SetCursorGrab()
    {
        if (!UseImageAsCursor())
        {
            return;
        }

        Cursor.SetCursor(grabCursorTexture, cursorCenter, CursorMode.Auto);
    }

    private bool UseImageAsCursor()
    {
        return settingsManager.Settings.GraphicSettings.useImageAsCursor;
    }
}
