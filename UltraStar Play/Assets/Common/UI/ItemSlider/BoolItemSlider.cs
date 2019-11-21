using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoolItemSlider : TextItemSlider<bool>
{
    private readonly string i18nCodeTrue = I18NKeys.yes;
    private readonly string i18nCodeFalse = I18NKeys.no;

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
