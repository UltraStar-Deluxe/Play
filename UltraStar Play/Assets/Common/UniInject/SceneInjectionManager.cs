using System;
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
        private readonly List<MonoBehaviour> scriptsThatNeedInjection = new List<MonoBehaviour>();

        private Injector sceneInjector;

        public bool logTime;

        void Awake()
        {
            Stopwatch stopwatch = CreateAndStartStopwatch();

            sceneInjector = UniInjectUtils.CreateInjector();
            UniInjectUtils.SceneInjector = sceneInjector;

            // (1) Iterate over scene hierarchy, thereby
            // (a) find IBinder instances.
            // (b) find scripts that need injection and how their members should be injected.
            AnalyzeScene();

            // (2) Store bindings in the sceneInjector
            CreateBindings();

            // (3) Inject the bindings from the sceneInjector into the objects that need injection.
            InjectScriptsThatNeedInjection();

            StopAndLogTime(stopwatch, $"SceneInjectionManager - Analyzing, binding and injecting scene {SceneManager.GetActiveScene().name} took {stopwatch.ElapsedMilliseconds} ms");
        }

        private void AnalyzeScene()
        {
            Stopwatch stopwatch = CreateAndStartStopwatch();

            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                AnalyzeScriptsRecursively(rootObject);

                IBinder[] bindersUnderRootObject = rootObject.GetComponentsInChildren<IBinder>();
                binders.AddRange(bindersUnderRootObject);
            }

            StopAndLogTime(stopwatch, $"SceneInjectionManager - Analyzing scene took {stopwatch.ElapsedMilliseconds} ms");
        }

        private void CreateBindings()
        {
            Stopwatch stopwatch = CreateAndStartStopwatch();

            foreach (IBinder binder in binders)
            {
                List<IBinding> bindings = binder.GetBindings();
                foreach (IBinding binding in bindings)
                {
                    sceneInjector.AddBinding(binding);
                }
            }

            StopAndLogTime(stopwatch, $"SceneInjectionManager - Creating bindings took {stopwatch.ElapsedMilliseconds} ms");
        }

        private void InjectScriptsThatNeedInjection()
        {
            Stopwatch stopwatch = CreateAndStartStopwatch();

            foreach (MonoBehaviour script in scriptsThatNeedInjection)
            {
                sceneInjector.Inject(script);
            }

            StopAndLogTime(stopwatch, $"SceneInjectionManager - Injecting scripts took {stopwatch.ElapsedMilliseconds} ms");
        }

        private Stopwatch CreateAndStartStopwatch()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            return stopwatch;
        }

        private void StopAndLogTime(Stopwatch stopwatch, string message)
        {
            stopwatch.Stop();
            if (logTime)
            {
                UnityEngine.Debug.Log(message);
            }
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

                List<InjectionData> injectionDatas = UniInjectUtils.GetInjectionDatas(script.GetType());
                if (injectionDatas.Count > 0)
                {
                    scriptsThatNeedInjection.Add(script);
                }
            }

            foreach (Transform child in gameObject.transform)
            {
                AnalyzeScriptsRecursively(child.gameObject);
            }
        }
    }
}