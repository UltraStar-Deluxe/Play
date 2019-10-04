using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeBarTimeLineRect : MonoBehaviour
{
    private Image background;

    void OnEnable()
    {
        background = GetComponent<Image>();
    }

    public void SetColor(Color color)
    {
        background.color = color;
    }
}
