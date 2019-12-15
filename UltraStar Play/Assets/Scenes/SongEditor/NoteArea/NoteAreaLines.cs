using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;

#pragma warning disable CS0649

public class NoteAreaLines : MonoBehaviour, INeedInjection
{
    public RectTransform linePrefab;

    public RectTransform uiLinesContainer;

    [Inject(searchMethod = SearchMethods.GetComponentInParent)]
    private NoteArea noteArea;

    void Start()
    {
        UpdateLines();
    }

    public void UpdateLines()
    {
        uiLinesContainer.DestroyAllDirectChildren<RectTransform>();

        int visibleMidiNoteCount = noteArea.GetVisibleMidiNoteCount();
        for (int i = 0; i < visibleMidiNoteCount; i++)
        {
            bool hasLine = (i % 2 == 0);
            if (hasLine)
            {
                CreateLine(i);
            }
        }
    }

    private void CreateLine(int midiNoteIndex)
    {
        RectTransform line = Instantiate(linePrefab, uiLinesContainer);

        float y = noteArea.GetVisibleMidiNotePositionY(midiNoteIndex);
        line.anchorMin = new Vector2(0, y);
        line.anchorMax = new Vector2(1, y);
        line.anchoredPosition = Vector2.zero;
        line.sizeDelta = new Vector2(0, line.sizeDelta.y);
    }
}
