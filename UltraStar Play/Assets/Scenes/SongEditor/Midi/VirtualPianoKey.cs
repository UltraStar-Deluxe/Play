using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;
using UniRx.Triggers;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VirtualPianoKey : MonoBehaviour, INeedInjection, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
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

            ColorBlock keyColors = keyButton.colors;
            if (MidiUtils.IsBlackPianoKey(midiNote))
            {
                keyColors.normalColor = Colors.black;
                keyColors.highlightedColor = Colors.grey;
                keyColors.pressedColor = Colors.darkGrey;
                keyColors.selectedColor = Colors.darkGrey;
                keyColors.disabledColor = Colors.red;
            }
            else
            {
                keyColors.normalColor = Colors.white;
                keyColors.highlightedColor = Colors.grey;
                keyColors.pressedColor = Colors.lightGrey;
                keyColors.selectedColor = Colors.lightGrey;
                keyColors.disabledColor = Colors.red;
            }
            keyButton.colors = keyColors;
        }
    }

    [InjectedInInspector]
    public Image keyImage;

    [InjectedInInspector]
    public Image micPitchIndicator;

    [Inject]
    private MidiManager midiManager;

    private Button keyButton;

    [Inject]
    private MicPitchTracker micPitchTracker;

    [Inject]
    private Settings settings;

    private IDisposable pitchEventStreamDisposable;

    void Awake()
    {
        keyButton = keyImage.GetComponent<Button>();
        micPitchIndicator.SetAlpha(0);
    }

    void Start()
    {
        pitchEventStreamDisposable = micPitchTracker.PitchEventStream.Subscribe(pitchEvent =>
        {
            if (pitchEvent == null)
            {
                micPitchIndicator.SetAlpha(0);
                return;
            }
            int shiftedMidiNote = pitchEvent.MidiNote + (settings.SongEditorSettings.MicOctaveOffset * 12);
            micPitchIndicator.SetAlpha(shiftedMidiNote == MidiNote ? 1 : 0);
        });
    }

    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == keyButton.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Needed for OnPointer events
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Needed for OnPointer events
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (midiNote > -1)
        {
            midiManager.PlayMidiNote(midiNote);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (midiNote > -1)
        {
            midiManager.StopMidiNote(midiNote);
        }
    }

    private void OnDestroy()
    {
        if (pitchEventStreamDisposable != null)
        {
            pitchEventStreamDisposable.Dispose();
        }
    }
}
