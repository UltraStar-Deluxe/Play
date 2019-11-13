using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Difficulty
{
    public static readonly Difficulty Easy = new Difficulty("difficulty.easy", 2);
    public static readonly Difficulty Medium = new Difficulty("difficulty.medium", 1);
    public static readonly Difficulty Hard = new Difficulty("difficulty.hard", 0);

    private string i18nCode;

    public string Name
    {
        get
        {
            return I18NManager.Instance.GetTranslation(i18nCode);
        }
    }

    public int RoundingDistance { get; private set; }

    private Difficulty(string i18nCode, int roundingDistance)
    {
        this.i18nCode = i18nCode;
        RoundingDistance = roundingDistance;
    }

    public static List<Difficulty> Values
    {
        get
        {
            return new List<Difficulty> { Easy, Medium, Hard };
        }
    }
}
