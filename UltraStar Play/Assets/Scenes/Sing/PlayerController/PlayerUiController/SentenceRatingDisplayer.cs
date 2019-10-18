using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SentenceRatingDisplayer : MonoBehaviour
{
    public SentenceRatingPopup sentenceRatingPopupPrefab;

    public void ShowSentenceRating(SentenceRating sentenceRating)
    {
        SentenceRatingPopup sentenceRatingPopup = Instantiate(sentenceRatingPopupPrefab);
        sentenceRatingPopup.GetComponent<RectTransform>().SetParent(GetComponent<RectTransform>());
        sentenceRatingPopup.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        sentenceRatingPopup.SetSentenceRating(sentenceRating);
    }
}
