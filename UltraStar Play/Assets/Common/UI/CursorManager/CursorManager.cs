using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

#pragma warning disable CS0649

public class CursorManager : AbstractSingletonBehaviour, INeedInjection
{
    public static CursorManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<CursorManager>();

    private static readonly int cursorWidth = 32;
    private static readonly int cursorHeight = 32;
    // hotSpot is the pixel coordinate in the texture where the actual cursor is measured.
    // Upper-left corner in the texture is coordinate (0,0).
    private static readonly Vector2 cursorTopLeftCorner = Vector2.zero;
    private static readonly Vector2 cursorCenter = new(cursorWidth / 2f, cursorHeight / 2f);
    private static readonly Vector2 lowerLeft = new(0, cursorHeight);

    [Inject]
    private SettingsManager settingsManager;

    // The texture must have type "Cursor" in Unity 3D import settings with maximum size 32px.
    public Texture2D cursorTexture;
    public Texture2D horizontalCursorTexture;
    public Texture2D verticalCursorTexture;
    public Texture2D grabCursorTexture;
    public Texture2D musicNoteCursorTexture;
    public Texture2D handCursorTexture;
    public Texture2D pencilCursorTexture;

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

        settingsManager.Settings
            .ObserveEveryValueChanged(it => it.UseImageAsCursor)
            .Subscribe(newValue => SetDefaultCursor())
            .AddTo(gameObject);
        SetDefaultCursor();
    }
    
    public static void SetCursorForVisualElement(VisualElement visualElement, ECursor cursor)
    {
        Instance.DoSetCursorForVisualElement(visualElement, cursor);
    }
    
    private void DoSetCursorForVisualElement(VisualElement visualElement, ECursor cursor)
    {
        visualElement.RegisterCallback<PointerEnterEvent>(_ => SetCursor(cursor));
        visualElement.RegisterCallback<PointerLeaveEvent>(_ => SetDefaultCursor());
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
            case ECursor.Hand:
                SetCursorHand();
                break;
            case ECursor.Pencil:
                SetCursorPencil();
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

    public void SetCursorHand()
    {
        if (!UseImageAsCursor())
        {
            return;
        }

        CurrentCursor = ECursor.Hand;
        SetCursor(handCursorTexture, cursorCenter, CursorMode.Auto);
    }

    public void SetCursorPencil()
    {
        if (!UseImageAsCursor())
        {
            return;
        }

        CurrentCursor = ECursor.Pencil;
        SetCursor(pencilCursorTexture, lowerLeft, CursorMode.Auto);
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
        return settingsManager.Settings.UseImageAsCursor;
    }

    private static void SetCursor(Texture2D texture, Vector2 hotspot, CursorMode cursorMode)
    {
        if (PlatformUtils.IsStandalone)
        {
            Cursor.SetCursor(texture, hotspot, cursorMode);
        }
    }
}
