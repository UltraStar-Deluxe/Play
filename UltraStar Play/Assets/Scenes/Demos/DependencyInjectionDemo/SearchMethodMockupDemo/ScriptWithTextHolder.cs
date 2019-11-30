using UnityEngine;
using UniInject;
using UnityEngine.UI;

// Ignore warnings about unassigned fields.
// Their values are injected, but this is not visible to the compiler.
#pragma warning disable CS0649

public class ScriptWithTextHolder : MonoBehaviour
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private readonly ITextHolder textHolder;

    public string GetText()
    {
        return textHolder.GetText();
    }
}