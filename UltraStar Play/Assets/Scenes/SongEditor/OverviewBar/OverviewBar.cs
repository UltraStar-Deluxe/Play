using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.EventSystems;

#pragma warning disable CS0649

public class OverviewBar : MonoBehaviour, IPointerClickHandler, INeedInjection
{
    [Inject(searchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    private float width;

    [Inject]
    private SongEditorSceneController songEditorSceneController;

    void Start()
    {
        width = rectTransform.rect.width;
    }

    // Scroll through the song via click on the overview bar.
    public void OnPointerClick(PointerEventData ped)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
                                                                     ped.position,
                                                                     ped.pressEventCamera,
                                                                     out Vector2 localPoint))
        {
            return;
        }

        double xPercent = (localPoint.x + (width / 2)) / width;
        songEditorSceneController.SetPositionInSongInPercent(xPercent);
    }
}
