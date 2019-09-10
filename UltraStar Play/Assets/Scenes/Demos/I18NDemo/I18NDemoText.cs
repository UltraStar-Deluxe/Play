using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class I18NDemoText : I18NText
{
    public string greetingName = "you";

    protected override Dictionary<string, string> GetTranslationArguments()
    {
        Dictionary<string, string> map = new Dictionary<string, string>();
        map.Add("name", greetingName);
        return map;
    }
}
