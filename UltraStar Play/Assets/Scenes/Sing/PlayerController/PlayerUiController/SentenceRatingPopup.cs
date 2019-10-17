using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SentenceRatingPopup : MonoBehaviour
{
    private float lifetime;

    void Awake()
    {
    }

    void Update()
    {
        if (lifetime > 1f)
        {
            Destroy(gameObject);
        }
        else
        {
            transform.Translate(0, 0.5f, 0);
            lifetime += Time.deltaTime;
        }
    }

    public void SetSentenceRating(SentenceRating sentenceRating)
    {
        GetComponentInChildren<Text>().text = sentenceRating.Text;
        GetComponentInChildren<Image>().color = sentenceRating.BackgroundColor;
    }
}
