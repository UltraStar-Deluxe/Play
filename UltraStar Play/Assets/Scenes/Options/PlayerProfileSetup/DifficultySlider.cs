using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DifficultySlider : TextItemSlider<Difficulty>
{
    protected override void Start()
    {
        base.Start();
        Items = Difficulty.Values;
    }

    protected override string GetDisplayString(Difficulty value)
    {
        if (value == null)
        {
            return "";
        }
        return value.Name;
    }
}
