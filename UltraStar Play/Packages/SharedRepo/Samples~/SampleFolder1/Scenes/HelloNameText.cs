using System.Collections;
using System.Collections.Generic;
using ProTrans;
using UnityEngine;

public class HelloNameText : TranslatedText
{
    public string nameValue = "Alice";
    
    protected override Dictionary<string, string> GetTranslationArguments()
    {
        // C# Dictionary initializer syntax: Each entry is written as {key, value}.
        return new Dictionary<string, string> { {"name", nameValue} };
    }
}
