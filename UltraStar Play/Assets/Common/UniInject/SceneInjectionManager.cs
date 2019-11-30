using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniInject
{
    public class SceneInjectionManager : MonoBehaviour
    {
        private readonly List<IBinder> binders = new List<IBinder>();
        private readonly List<InjectionData> injectionDatas = new List<InjectionData>();

        private Injector sceneInjector;

        void Awake()
        {
            sceneInjector = UniInject.CreateInjector();
            UniInject.SceneInjector = sceneInjector;

            // (1) Iterate over scene hierarchy, thereby
            // (a) find IBinder instances.
            // (b) find objects that need injection and how their members should be injected.
            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                AnalyzeScriptsRecursively(rootObject);
            }

            // (2) Store bindings in the SceneInjector
            foreach (IBinder binder in binders)
            {
                List<IBinding> bindings = binder.GetBindings();
                foreach (IBinding binding in bindings)
                {
                    sceneInjector.AddBinding(binding);
                }
            }

            // (4) Inject the bindings from the SceneInjector into the objects that need injection.
            foreach (InjectionData injectionData in injectionDatas)
            {
                sceneInjector.Inject(injectionData);
            }
        }

        private void AnalyzeScriptsRecursively(GameObject gameObject)
        {
            MonoBehaviour[] scripts = gameObject.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts.Where(it => it != null))
            {
                if (script is IBinder)
                {
                    binders.Add(script as IBinder);
                }
                List<InjectionData> newInjectionDatas = ReflectionUtils.CreateInjectionDatas(script);
                injectionDatas.AddRange(newInjectionDatas);
            }

            foreach (Transform child in gameObject.transform)
            {
                AnalyzeScriptsRecursively(child.gameObject);
            }
        }
    }
}