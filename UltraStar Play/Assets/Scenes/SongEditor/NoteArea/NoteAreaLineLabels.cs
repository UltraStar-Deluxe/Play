using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

public class NoteAreaLineLabels : MonoBehaviour, INeedInjection
{
    public Text labelPrefab;

    public RectTransform uiLabelsContainer;

    [Inject(searchMethod = SearchMethods.GetComponentInParent)]
    private NoteArea noteArea;

    void Start()
    {
        UpdateLabels();
    }

    public void UpdateLabels()
    {
        uiLabelsContainer.DestroyAllDirectChildren<RectTransform>();

        int visibleMidiNoteCount = noteArea.GetVisibleMidiNoteCount();
        for (int i = 0; i < visibleMidiNoteCount; i++)
        {
            CreateLabel(i);
        }
    }

    private void CreateLabel(int midiNoteIndexInViewport)
    {
        Text uiText = Instantiate(labelPrefab, uiLabelsContainer);
        RectTransform label = uiText.GetComponent<RectTransform>();

        float y = noteArea.GetVisibleMidiNotePositionY(midiNoteIndexInViewport);
        float anchorHeight = 1f / noteArea.viewportHeight;
        label.anchorMin = new Vector2(0, y - (anchorHeight / 2f));
        label.anchorMax = new Vector2(1, y + (anchorHeight / 2f));
        label.anchoredPosition = Vector2.zero;
        label.sizeDelta = new Vector2(0, 0);

        int midiNote = noteArea.GetMidiNote(midiNoteIndexInViewport);
        string midiNoteName = MidiUtils.GetAbsoluteName(midiNote);
        uiText.text = midiNoteName;
    }
}
