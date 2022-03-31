using UniInject;
using UniRx;
using UnityEngine;

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
    private static readonly Vector2 cursorCenter = new(cursorWidth / 2f, cursorWidth / 2f);

    [Inject]
    private SettingsManager settingsManager;

    // The texture must have type "Cursor" in Unity 3D import settings.
    public Texture2D cursorTexture;
    public Texture2D horizontalCursorTexture;
    public Texture2D verticalCursorTexture;
    public Texture2D grabCursorTexture;
    public Texture2D musicNoteCursorTexture;

    public ECursor CurrentCursor { get; private set; } = ECursor.Default;

    private void Start()
    {
        settingsManager.Settings.GraphicSettings
            .ObserveEveryValueChanged(it => it.useImageAsCursor)
            .Subscribe(newValue => SetDefaultCursor())
            .AddTo(gameObject);
        SetDefaultCursor();
    }

    public void SetCursor(ECursor cursor)
    {
        if (CurrentCursor == cursor)
        {
            return;
        }

        switch (cursor)
        {
            case ECursor.Default:
                SetDefaultCursor();
                break;
            case ECursor.Grab:
                SetCursorGrab();
                break;
            case ECursor.ArrowsLeftRight:
                SetCursorHorizontal();
                break;
            case ECursor.ArrowsUpDown:
                SetCursorVertical();
                break;
            case ECursor.MusicNote:
                SetCursorMusicNote();
                break;
            default:
                Debug.LogWarning("Unkown cursor: " + cursor);
                break;
        }
    }

    public void SetDefaultCursor()
    {
        CurrentCursor = ECursor.Default;
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

        CurrentCursor = ECursor.ArrowsLeftRight;
        Cursor.SetCursor(horizontalCursorTexture, cursorCenter, CursorMode.Auto);
    }

    public void SetCursorMusicNote()
    {
        if (!UseImageAsCursor())
        {
            return;
        }

        CurrentCursor = ECursor.MusicNote;
        Cursor.SetCursor(musicNoteCursorTexture, cursorCenter, CursorMode.Auto);
    }

    public void SetCursorVertical()
    {
        if (!UseImageAsCursor())
        {
            return;
        }

        CurrentCursor = ECursor.ArrowsUpDown;
        Cursor.SetCursor(verticalCursorTexture, cursorCenter, CursorMode.Auto);
    }

    public void SetCursorGrab()
    {
        CurrentCursor = ECursor.Grab;
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
