using System.Collections;
using System.Collections.Generic;
using UniInject;
using static UniInject.UniInjectUtils;
using UnityEngine;

public class SearchMethodMockupDemo : MonoBehaviour
{
    public ScriptWithTextHolder prefab;

    void Start()
    {
        // Manual injection with dummy component.
        // This can be useful for tests because the tests do not need the real scene hierarchy anymore.
        ScriptWithTextHolder dummyInstance = Instantiate(prefab);
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
}

