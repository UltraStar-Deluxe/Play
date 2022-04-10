using System;
using System.Collections;
using System.Collections.Generic;
using ProTrans;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class GetTranslationExampleText : AbstractTranslatorBehaviour
{
    public string city;
    public int age = 20;
    //
    public override void UpdateTranslation()
    {
        // Static method to access translations.
        // In this example, a generated constant is used to access the translation (avoids typos and makes refactoring easier).
        // The path to the generated resources folder is specified in the TranslationManager.
        
        // After the translation key, further arguments can be given in the form [key1, value1, key2, value2, ...].
        GetComponent<Text>().text = TranslationManager.GetTranslation(R.Messages.sampleScene_staticMethodAccessExample,
            "city", city,
            "age", age);
    }
}
