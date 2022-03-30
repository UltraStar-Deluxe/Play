using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VirtualPianoKeyControl : INeedInjection, IInjectionFinishedListener
{
    private int midiNote = -1;
    public int MidiNote
    {
        get
        {
            return midiNote;
        }
        set
        {
            if (midiNote != -1)
            {
                midiManager.StopMidiNote(midiNote);
            }

            midiNote = value;

            if (MidiUtils.IsBlackPianoKey(midiNote))
            {
                VisualElement.RemoveFromClassList("whiteKey");
                VisualElement.AddToClassList("blackKey");
            }
            else
            {
                VisualElement.AddToClassList("whiteKey");
                VisualElement.RemoveFromClassList("blackKey");
            }
        }
    }

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    [Inject]
    private MidiManager midiManager;

    public void OnInjectionFinished()
    {
        VisualElement.RegisterCallback<PointerDownEvent>(evt => PlayMidiNote());
        VisualElement.RegisterCallback<PointerUpEvent>(evt => StopMidiNote());
        VisualElement.RegisterCallback<PointerLeaveEvent>(evt => StopMidiNote());
    }

    private void PlayMidiNote()
    {
        StopMidiNote();
        if (midiNote > -1)
        {
            midiManager.PlayMidiNote(midiNote);
        }
    }

    private void StopMidiNote()
    {
        if (midiNote > -1)
        {
            midiManager.StopMidiNote(midiNote);
        }
    }

    public void Show()
    {
        VisualElement.ShowByDisplay();
    }

    public void Hide()
    {
        VisualElement.HideByDisplay();
    }
}
