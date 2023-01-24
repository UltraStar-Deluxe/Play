using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UniInject;
using UniInject.Extensions;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using IBinding = UniInject.IBinding;

public class UltraStarPlaySceneInjectionManager : MonoBehaviour
{
    public ESceneInjectionStatus SceneInjectionStatus { get; private set; } = ESceneInjectionStatus.Pending;

    private readonly List<IBinder> binders = new();
    private readonly List<UnityEngine.Object> scriptsThatNeedInjection = new();

    private readonly List<ISceneInjectionFinishedListener> sceneInjectionFinishedListeners = new();

    private Injector sceneInjector;

    [InjectedInInspector]
    public bool logTime;

    public static UltraStarPlaySceneInjectionManager Instance => GameObjectUtils.FindComponentWithTag<UltraStarPlaySceneInjectionManager>("SceneInjectionManager");

    private void Awake()
    {
        if (SceneInjectionStatus != ESceneInjectionStatus.Pending)
        {
            return;
        }

        DoSceneInjection();
    }

    public void DoSceneInjection()
    {
        if (SceneInjectionStatus != ESceneInjectionStatus.Pending)
        {
            Debug.LogWarning("Attempt to redo scene injection.");
            return;
        }
        SceneInjectionStatus = ESceneInjectionStatus.Started;

        Stopwatch stopwatch = CreateAndStartStopwatch();

        sceneInjector = UniInjectUtils.CreateInjector();

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

        SceneInjectionStatus = ESceneInjectionStatus.Finished;

        StopAndLogTime(stopwatch, $"SceneInjectionManager - Analyzing, binding and injecting scene took <ms> ms");

        // (4) Notify listeners that scene injection has finished
        foreach (ISceneInjectionFinishedListener listener in sceneInjectionFinishedListeners)
        {
            listener.OnSceneInjectionFinished();
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

        // Explicitly analyze the DontDestroyOnLoad objects. These are not included in scene.GetRootGameObjects.
        AnalyzeScriptsRecursively(DontDestroyOnLoadManager.Instance.gameObject);

        StopAndLogTime(stopwatch, $"SceneInjectionManager - Analyzing scene {scene.name} took <ms> ms");
    }

    private void CreateBindings()
    {
        Stopwatch stopwatch = CreateAndStartStopwatch();

        foreach (IBinder binder in binders)
        {
            List<IBinding> bindings = binder.GetBindings();
            foreach (IBinding binding in bindings)
            {
                try
                {
                    sceneInjector.AddBinding(binding, RebindingBehavior.Throw);
                }
                catch (RebindingException ex)
                {
                    Debug.LogWarning($"{ex.Message} while processing {binder}");
                }
            }
        }

        StopAndLogTime(stopwatch, $"SceneInjectionManager - Creating bindings took <ms> ms");
    }

    private void InjectScriptsThatNeedInjection()
    {
        Stopwatch stopwatch = null;
        if (logTime)
        {
            stopwatch = CreateAndStartStopwatch();
        }

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

        StopAndLogTime(stopwatch, $"SceneInjectionManager - Injecting scripts took <ms> ms");
    }

    private Stopwatch CreateAndStartStopwatch()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        return stopwatch;
    }

    private void StopAndLogTime(Stopwatch stopwatch, string message)
    {
        if (stopwatch != null)
        {
            stopwatch.Stop();
            Debug.Log(message.Replace("<ms>", stopwatch.ElapsedMilliseconds.ToString()));
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

            if (script is INeedInjection
                and not IExcludeFromSceneInjection)
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

    public enum ESceneInjectionStatus
    {
        Pending,
        Started,
        Finished
    }
}
