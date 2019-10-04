using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeBarTimeLinePositionIndicator : MonoBehaviour
{
    private RectTransform rectTransform;
    private SingSceneController singSceneController;

    void OnEnable()
    {
        singSceneController = SingSceneController.Instance;
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        double xPos = singSceneController.PositionInSongInMillis / singSceneController.DurationOfSongInMillis;
        rectTransform.anchorMin = new Vector2((float)xPos, rectTransform.anchorMin.y);
        rectTransform.anchorMax = new Vector2((float)xPos, rectTransform.anchorMax.y);
        rectTransform.anchoredPosition = Vector2.zero;
    }
}
