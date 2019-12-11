using System.Collections;
using System.Collections.Generic;
using UniInject;
using static UniInject.UniInjectUtils;
using UnityEngine;

// Ignore warnings about unassigned fields.
// Their values are injected, but this is not visible to the compiler.
#pragma warning disable CS0649
public class SearchMethodMockupDemo : MonoBehaviour
{
    void Start()
    {
        // Manual injection with dummy component.
        // This can be useful for tests because the tests do not need the real scene hierarchy anymore.
        GameObject gameObject = new GameObject();
        ScriptThatNeedsInjectionOfTextHolder dummyInstance = gameObject.AddComponent<ScriptThatNeedsInjectionOfTextHolder>();
        GlobalInjector.MockUnitySearchMethod(dummyInstance, SearchMethods.GetComponentInChildren, new TextHolderMock());
        GlobalInjector.Inject(dummyInstance);

        Debug.Log("The component to be tested returns: " + dummyInstance.GetText());
    }

    private class TextHolderMock : ITextHolder
    {
        public string GetText()
        {
            return "Hi, I am TextHolderMock";
        }
    }

    private class ScriptThatNeedsInjectionOfTextHolder : MonoBehaviour
    {
        [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
        private readonly ITextHolder textHolder;

        public string GetText()
        {
            return textHolder.GetText();
        }
    }
}

