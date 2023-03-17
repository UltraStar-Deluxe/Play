using UniInject;
using UniRx;
using UnityEngine;

#pragma warning disable CS0649

public class CursorManager : AbstractSingletonBehaviour, INeedInjection
{
    public static CursorManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<CursorManager>();

    private static readonly int cursorWidth = 32;
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

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        if (Instance != this)
        {
            return;
        }

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
            SetCursor(cursorTexture, cursorTopLeftCorner, CursorMode.Auto);
        }
        else
        {
            SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    public void SetCursorHorizontal()
    {
        if (!UseImageAsCursor())
        {
            return;
        }

        CurrentCursor = ECursor.ArrowsLeftRight;
        SetCursor(horizontalCursorTexture, cursorCenter, CursorMode.Auto);
    }

    public void SetCursorMusicNote()
    {
        if (!UseImageAsCursor())
        {
            return;
        }

        CurrentCursor = ECursor.MusicNote;
        SetCursor(musicNoteCursorTexture, cursorCenter, CursorMode.Auto);
    }

    public void SetCursorVertical()
    {
        if (!UseImageAsCursor())
        {
            return;
        }

        CurrentCursor = ECursor.ArrowsUpDown;
        SetCursor(verticalCursorTexture, cursorCenter, CursorMode.Auto);
    }

    public void SetCursorGrab()
    {
        CurrentCursor = ECursor.Grab;
        if (!UseImageAsCursor())
        {
            return;
        }

        SetCursor(grabCursorTexture, cursorCenter, CursorMode.Auto);
    }

    private bool UseImageAsCursor()
    {
        return settingsManager.Settings.GraphicSettings.useImageAsCursor;
    }

    private static void SetCursor(Texture2D texture, Vector2 hotspot, CursorMode cursorMode)
    {
        if (PlatformUtils.IsStandalone)
        {
            Cursor.SetCursor(texture, hotspot, cursorMode);
        }
    }
}
