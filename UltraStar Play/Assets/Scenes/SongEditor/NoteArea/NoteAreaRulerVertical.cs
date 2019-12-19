using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

public class NoteAreaRulerVertical : MonoBehaviour, INeedInjection
{
    public Text labelPrefab;
    public RectTransform labelContainer;

    public RectTransform linePrefab;
    public RectTransform lineContainer;

    [Inject(searchMethod = SearchMethods.GetComponentInParent)]
    private readonly NoteArea noteArea;

    void Start()
    {
        UpdatePitchLinesAndLabels();
    }

    public void UpdatePitchLinesAndLabels()
    {
        labelContainer.DestroyAllDirectChildren();
        lineContainer.DestroyAllDirectChildren();

        int visibleMidiNoteCount = noteArea.GetVisibleMidiNoteCount();
        for (int i = 0; i < visibleMidiNoteCount; i++)
        {
            CreateLabelForMidiNote(i);
            // Notes are drawn on lines and between lines alternatingly.
            bool hasLine = (i % 2 == 0);
            if (hasLine)
            {
                CreateLineForMidiNote(i);
            }
        }
    }

    private void CreateLabelForMidiNote(int midiNoteIndexInViewport)
    {
        Text uiText = Instantiate(labelPrefab, labelContainer);
        RectTransform label = uiText.GetComponent<RectTransform>();

        float y = noteArea.GetVerticalPositionForIndexInViewport(midiNoteIndexInViewport);
        float anchorHeight = noteArea.GetHeightForSingleNote();
        label.anchorMin = new Vector2(0, y - (anchorHeight / 2f));
        label.anchorMax = new Vector2(1, y + (anchorHeight / 2f));
        label.anchoredPosition = Vector2.zero;
        label.sizeDelta = new Vector2(0, 0);

        int midiNote = noteArea.GetMidiNote(midiNoteIndexInViewport);
        string midiNoteName = MidiUtils.GetAbsoluteName(midiNote);
        uiText.text = midiNoteName;
    }

    private void CreateLineForMidiNote(int midiNoteIndexInViewport)
    {
        RectTransform line = Instantiate(linePrefab, lineContainer);

        float y = noteArea.GetVerticalPositionForIndexInViewport(midiNoteIndexInViewport);
        line.anchorMin = new Vector2(0, y);
        line.anchorMax = new Vector2(1, y);
        line.anchoredPosition = Vector2.zero;
        line.sizeDelta = new Vector2(0, line.sizeDelta.y);
    }
}
