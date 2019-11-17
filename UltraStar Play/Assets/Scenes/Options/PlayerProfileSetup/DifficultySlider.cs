using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DifficultySlider : TextItemSlider<EDifficulty>
{
    protected override void Start()
    {
        base.Start();
        Items = EnumUtils.GetValuesAsList<EDifficulty>();
    }

    protected override string GetDisplayString(EDifficulty value)
    {
        return value.GetTranslatedName();
    }
}
