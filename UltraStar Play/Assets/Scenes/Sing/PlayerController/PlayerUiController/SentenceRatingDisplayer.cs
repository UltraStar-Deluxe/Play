using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SentenceRatingDisplayer : MonoBehaviour
{
    public SentenceRatingPopup sentenceRatingPopupPrefab;

    public void ShowSentenceRating(SentenceRating sentenceRating)
    {
        SentenceRatingPopup sentenceRatingPopup = Instantiate(sentenceRatingPopupPrefab, transform);
        sentenceRatingPopup.GetComponent<RectTransform>().MoveCornersToAnchors();
        sentenceRatingPopup.SetSentenceRating(sentenceRating);
    }
}
