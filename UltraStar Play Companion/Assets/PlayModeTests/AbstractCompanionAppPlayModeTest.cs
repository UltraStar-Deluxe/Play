using System.Collections;
using System.Collections.Generic;
using UniInject;
using UniInject.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static ConditionUtils;

public abstract class AbstractCompanionAppPlayModeTest : AbstractInputSystemTest
{
    protected virtual string TestSceneName => "MainScene";

    [UnitySetUp]
    public IEnumerator UnitySetUp() => UnitySetUpAsync();
    private async Awaitable UnitySetUpAsync()
    {
        await SetUpTestFixtureAsync();
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown() => UnityTearDownAsync();
    private async Awaitable UnityTearDownAsync()
    {
        await TearDownTestFixtureAsync();
    }

    protected virtual async Awaitable SetUpTestFixtureAsync()
    {
        Debug.Log($"{this}.{nameof(SetUpTestFixtureAsync)}");

        InputFixture = new InputTestFixture();
        Keyboard = InputSystem.GetDevice<Keyboard>();

        await LoadTestSceneAsync();

        Injector testInjector = UltraStarPlaySceneInjectionManager.Instance.SceneInjector.CreateChildInjector();
        AddPageObjectBindings(testInjector);
        testInjector.Inject(this);
    }

    private void AddPageObjectBindings(Injector injector)
    {
        // injector.AddBindingForInstance(injector.CreateAndInject<SongListPageObject>(), RebindingBehavior.Throw);
        // injector.AddBindingForInstance(injector.CreateAndInject<SongDetailsPageObject>(), RebindingBehavior.Throw);
        // injector.AddBindingForInstance(injector.CreateAndInject<SongQueuePageObject>(), RebindingBehavior.Throw);

        injector.AddBinding(new Binding(typeof(SongListPageObject), new NewInstancesProvider(typeof(SongListPageObject))), RebindingBehavior.Throw);
        injector.AddBinding(new Binding(typeof(SongDetailsPageObject), new NewInstancesProvider(typeof(SongDetailsPageObject))), RebindingBehavior.Throw);
        injector.AddBinding(new Binding(typeof(SongQueuePageObject), new NewInstancesProvider(typeof(SongQueuePageObject))), RebindingBehavior.Throw);
    }

    protected virtual async Awaitable TearDownTestFixtureAsync()
    {
        Debug.Log($"{this}.{nameof(TearDownTestFixtureAsync)}");
        await DeleteAllGameObjectsAsync();
    }

    protected virtual async Awaitable LoadTestSceneAsync()
    {
        if (TestSceneName.IsNullOrEmpty())
        {
            Debug.Log("Skip loading test scene, TestSceneName is null or empty.");
            return;
        }

        await LoadSceneByNameAsync(TestSceneName);
    }

    private async Awaitable LoadSceneByNameAsync(string sceneName)
    {
        if (sceneName.IsNullOrEmpty())
        {
            return;
        }

        Debug.Log($"Loading test scene {sceneName}");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        await WaitForConditionAsync(
            () => SceneManager.GetActiveScene().name == sceneName,
            new WaitForConditionConfig {description = $"test scene loaded: sceneName '{sceneName}'"});

        await WaitForConditionAsync(
            () => DontDestroyOnLoadManager.Instance != null,
            new WaitForConditionConfig { description = "DontDestroyOnLoadManager instance is present" });
    }

    private async Awaitable DeleteAllGameObjectsAsync()
    {
        List<GameObject> gameObjects = new List<GameObject>();

        // Destroy regular objects in scene
        foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            gameObjects.Add(gameObject);
        }

        // Destroy DontDestroyOnLoad objects
        if (DontDestroyOnLoadManager.Instance != null)
        {
            gameObjects.Add(DontDestroyOnLoadManager.Instance.gameObject);
        }

        foreach (GameObject gameObject in gameObjects)
        {
            GameObject.Destroy(gameObject);
        }

        // Wait for objects to be destroyed
        await WaitForConditionAsync(() =>
        {
            Debug.Log("Waiting for GameObjects to be destroyed.");
            return gameObjects.AllMatch(destroyedGameObject => destroyedGameObject == null);
        });
        Debug.Log("All GameObjects have been destroyed.");

        await Awaitable.NextFrameAsync();
    }
}
