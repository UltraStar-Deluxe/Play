using NUnit.Framework;
using UniInject;
using UnityEngine;

namespace UniInject.Tests
{

    public class UnitySearchMethodMockupTests
    {
        [Test]
        public void SearchMethodMockupTest()
        {
            Injector injector = UniInjectUtils.CreateInjector();

            GameObject gameObject = new GameObject();
            ScriptThatNeedsInjectionFromSceneHierarchy script = gameObject.AddComponent<ScriptThatNeedsInjectionFromSceneHierarchy>();

            injector.MockUnitySearchMethod(script, SearchMethods.GetComponent, new TextHolderImpl("sibling"));
            injector.MockUnitySearchMethod(script, SearchMethods.GetComponentInChildren, new TextHolderImpl("child"));
            injector.MockUnitySearchMethod(script, SearchMethods.GetComponentInParent, new TextHolderImpl("parent"));
            injector.MockUnitySearchMethod(script, SearchMethods.FindObjectOfType, new TextHolderImpl("other"));

            // Because the search methods have been mocked, the implementation of the test ist returned.
            // If no mock is present, then the normal corresponding Unity method is called to resolve the dependency.
            injector.Inject(script);

            Assert.AreEqual("sibling", script.siblingComponent.GetText());
            Assert.AreEqual("child", script.childComponent.GetText());
            Assert.AreEqual("parent", script.parentComponent.GetText());
            Assert.AreEqual("other", script.otherComponent.GetText());

            GameObject.DestroyImmediate(gameObject);
        }

        [Test]
        public void NullMatchesAllScripts()
        {
            Injector injector = UniInjectUtils.CreateInjector();

            GameObject gameObject1 = new GameObject();
            ScriptThatNeedsInjectionFromSceneHierarchy script1 = gameObject1.AddComponent<ScriptThatNeedsInjectionFromSceneHierarchy>();

            GameObject gameObject2 = new GameObject();
            ScriptThatNeedsInjectionFromSceneHierarchy script2 = gameObject1.AddComponent<ScriptThatNeedsInjectionFromSceneHierarchy>();

            // GetComponent is mocked only for script1.
            injector.MockUnitySearchMethod(script1, SearchMethods.GetComponent, new TextHolderImpl("sibling"));
            // FindObjectOfType is mocked for all scripts.
            injector.MockUnitySearchMethod(null, SearchMethods.FindObjectOfType, new TextHolderImpl("other"));

            injector.Inject(script1);
            injector.Inject(script2);

            // Check the values of script1
            Assert.AreEqual("sibling", script1.siblingComponent.GetText());
            Assert.IsNull(script1.childComponent);
            Assert.IsNull(script1.parentComponent);
            Assert.AreEqual("other", script1.otherComponent.GetText());

            // Check the values of script2
            Assert.IsNull(script2.siblingComponent);
            Assert.IsNull(script2.childComponent);
            Assert.IsNull(script2.parentComponent);
            Assert.AreEqual("other", script2.otherComponent.GetText());

            GameObject.DestroyImmediate(gameObject1);
            GameObject.DestroyImmediate(gameObject2);
        }

        public class ScriptThatNeedsInjectionFromSceneHierarchy : MonoBehaviour
        {
            [Inject(searchMethod = SearchMethods.GetComponent, optional = true)]
            public ITextHolder siblingComponent;

            [Inject(searchMethod = SearchMethods.GetComponentInChildren, optional = true)]
            public ITextHolder childComponent;

            [Inject(searchMethod = SearchMethods.GetComponentInParent, optional = true)]
            public ITextHolder parentComponent;

            [Inject(searchMethod = SearchMethods.FindObjectOfType)]
            public ITextHolder otherComponent;
        }

        /////////////////////////////////////////////////////
        public interface ITextHolder
        {
            string GetText();
        }

        public class TextHolderImpl : ITextHolder
        {
            private string text;

            public TextHolderImpl(string text)
            {
                this.text = text;
            }

            public string GetText()
            {
                return text;
            }
        }
    }
}