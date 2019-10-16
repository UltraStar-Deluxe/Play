using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ArtistText : MonoBehaviour
{
    private Text text;

    void Awake()
    {
        text = GetComponent<Text>();
    }

    public void SetText(string value)
    {
        // Make the text bold
        text.text = "<b>" + value + "</b>";
    }
}
