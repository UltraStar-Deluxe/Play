using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniInject
{
    public class SceneInjectionManager : MonoBehaviour
    {
        private readonly List<IBinder> binders = new List<IBinder>();
        private readonly List<UnityEngine.Object> scriptsThatNeedInjection = new List<UnityEngine.Object>();

        private readonly List<ISceneInjectionFinishedListener> sceneInjectionFinishedListeners = new List<ISceneInjectionFinishedListener>();

        private Injector sceneInjector;

        [Tooltip("Only inject scripts with marker interface INeedInjection")]
        public bool onlyInjectScriptsWithMarkerInterface;

        public bool logTime;

        void Awake()
        {
            Stopwatch stopwatch = CreateAndStartStopwatch();

            sceneInjector = UniInjectUtils.CreateInjector();
            UniInjectUtils.SceneInjector = sceneInjector;

            // Bind the scene injector itself.
            // This way it can be injected at the scene start
            // and be used to inject newly created scripts at runtime.
            sceneInjector.AddBindingForInstance(sceneInjector);

            // (1) Iterate over scene hierarchy, thereby
            // (a) find IBinder instances.
            // (b) find scripts that need injection and how their members should be injected.
            AnalyzeScene();

            // (2) Store bindings in the sceneInjector
            CreateBindings();

            // (3) Inject the bindings from the sceneInjector into the objects that need injection.
            InjectScriptsThatNeedInjection();

            StopAndLogTime(stopwatch, $"SceneInjectionManager - Analyzing, binding and injecting scene took {stopwatch.ElapsedMilliseconds} ms");

            // (4) Notify listeners that scene injection has finished
            foreach (ISceneInjectionFinishedListener listener in sceneInjectionFinishedListeners)
            {
                listener.OnSceneInjectionFinished();
            }
        }

        void OnDestroy()
        {
            if (UniInjectUtils.SceneInjector == sceneInjector)
            {
                UniInjectUtils.SceneInjector = null;
            }
        }

        private void AnalyzeScene()
        {
            Stopwatch stopwatch = CreateAndStartStopwatch();

            Scene scene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                AnalyzeScriptsRecursively(rootObject);
            }

            StopAndLogTime(stopwatch, $"SceneInjectionManager - Analyzing scene {scene.name} took {stopwatch.ElapsedMilliseconds} ms");
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

            foreach (UnityEngine.Object script in scriptsThatNeedInjection)
            {
                try
                {
                    sceneInjector.Inject(script);
                }
                catch (InjectionException e)
                {
                    UnityEngine.Debug.LogException(e, script);
                    // Continue injection of other scripts.
                }
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
                // The script can be null if it is a missing component.
                if (script == null)
                {
                    continue;
                }

                // Analyzing a type for InjectionData is costly.
                // The types of the UnityEngine do not make use of UniInject.
                // Thus, the scripts from the UnityEngine itself should be skipped for better performance.
                Type type = script.GetType();
                if (!string.IsNullOrEmpty(type.Namespace) && type.Namespace.StartsWith("UnityEngine."))
                {
                    continue;
                }

                if (script is IBinder)
                {
                    binders.Add(script as IBinder);
                }

                if (script is ISceneInjectionFinishedListener)
                {
                    sceneInjectionFinishedListeners.Add(script as ISceneInjectionFinishedListener);
                }

                if (!onlyInjectScriptsWithMarkerInterface || script is INeedInjection)
                {
                    List<InjectionData> injectionDatas = UniInjectUtils.GetInjectionDatas(script.GetType());
                    if (injectionDatas.Count > 0)
                    {
                        scriptsThatNeedInjection.Add(script);
                    }
                }
            }

            foreach (Transform child in gameObject.transform)
            {
                AnalyzeScriptsRecursively(child.gameObject);
            }
        }
    }
}