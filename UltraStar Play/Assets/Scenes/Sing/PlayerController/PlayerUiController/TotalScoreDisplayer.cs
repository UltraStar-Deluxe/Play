using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TotalScoreDisplayer : MonoBehaviour
{
    private Text text;

    void Awake()
    {
        text = GetComponentInChildren<Text>();
    }

    internal void ShowTotalScore(int score)
    {
        text.text = score.ToString();
    }
}
