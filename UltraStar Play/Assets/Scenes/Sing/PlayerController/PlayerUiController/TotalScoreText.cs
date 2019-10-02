using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class TotalScoreText : MonoBehaviour
{
    private Text text;

    void OnEnable()
    {
        text = GetComponent<Text>();
    }

    public void SetScore(int score)
    {
        text.text = score.ToString();
    }
}
