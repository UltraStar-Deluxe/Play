using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SentenceRatingPopup : MonoBehaviour
{
    private float lifetime;

    public SentenceRating SentenceRating
    {
        set
        {
            GetComponentInChildren<Text>().text = value.Text;
            GetComponentInChildren<Image>().color = value.BackgroundColor;
        }
    }

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
}
