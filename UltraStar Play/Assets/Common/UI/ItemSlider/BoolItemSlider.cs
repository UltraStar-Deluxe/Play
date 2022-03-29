using System.Collections.Generic;
using ProTrans;

public class BoolItemSlider : TextItemSlider<bool>
{
    private readonly string i18nCodeTrue = R.Messages.yes;
    private readonly string i18nCodeFalse = R.Messages.no;

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
        return TranslationManager.GetTranslation(item ? i18nCodeTrue : i18nCodeFalse);
    }
}
