using System.Collections.Generic;
using System.Diagnostics;
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

        // Only for development: measure time of scene injection
#if UNITY_EDITOR
        public bool logTime;
        private Stopwatch stopwatch = new Stopwatch();
#endif

        void Awake()
        {
#if UNITY_EDITOR
            stopwatch.Start();
#endif

            sceneInjector = UniInjectUtils.CreateInjector();
            UniInjectUtils.SceneInjector = sceneInjector;

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

#if UNITY_EDITOR
            stopwatch.Stop();
            if (logTime)
            {
                UnityEngine.Debug.Log($"Injection of scene {SceneManager.GetActiveScene().name} done in {stopwatch.ElapsedMilliseconds} ms");
            }
#endif
        }

        private void AnalyzeScriptsRecursively(GameObject gameObject)
        {
            MonoBehaviour[] scripts = gameObject.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                // If the script is null, then this is a missing component
                if (script == null)
                {
                    continue;
                }

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