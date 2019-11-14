using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoolItemSlider : TextItemSlider<bool>
{
    private readonly string i18nCodeTrue = "yes";
    private readonly string i18nCodeFalse = "no";

    protected override void Start()
    {
        base.Start();
        List<bool> boolItemsList = new List<bool>();
        boolItemsList.Add(false);
        boolItemsList.Add(true);
        Items = boolItemsList;
    }

    protected override string GetDisplayString(bool item)
    {
        return I18NManager.Instance.GetTranslation(item ? i18nCodeTrue : i18nCodeFalse);
    }
}
