using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniInject.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class SceneRecipeManager : AbstractSingletonBehaviour, INeedInjection
{
    public static SceneRecipeManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SceneRecipeManager>();

    [InjectedInInspector]
    public List<SceneRecipe> sceneRecipes;

    [Inject]
    private UIDocument uiDocument;
    
    [Inject]
    private Injector injector;

    private SceneRecipe loadedSceneRecipe;
        
    protected override object GetInstance()
    {
        return Instance;
    }

    public SceneRecipe GetSceneRecipe(EScene scene)
    {
        return sceneRecipes.FirstOrDefault(it => it.scene == scene);
    }

    public void LoadSceneFromRecipe(SceneRecipe sceneRecipe)
    {
        UnloadScene();
        loadedSceneRecipe = sceneRecipe;

        Debug.Log($"Loading scene recipe {sceneRecipe.scene}");

        // Load UI
        VisualElement loadedSceneVisualElement = loadedSceneRecipe.visualTreeAsset.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Clear();
        uiDocument.rootVisualElement.Add(loadedSceneVisualElement);

        // Instantiate GameObjects in scene.
        List<GameObject> loadedGameObjects = new();
        foreach (GameObject loadedGameObjectRecipe in loadedSceneRecipe.sceneGameObjects)
        {
            GameObject loadedGameObject = Instantiate(loadedGameObjectRecipe);
            loadedGameObjects.Add(loadedGameObject);
        }

        // Add new bindings from loaded objects.
        Injector loadedSceneInjector = injector.CreateChildInjector();
        foreach (GameObject loadedGameObject in loadedGameObjects)
        {
            foreach (IBinder binder in loadedGameObject.GetComponentsInChildren<IBinder>())
            {
                binder.GetBindings().ForEach(binding => loadedSceneInjector.AddBinding(binding));
            }
        }

        // Inject loaded objects
        foreach (GameObject loadedGameObject in loadedGameObjects)
        {
            loadedSceneInjector
                .WithRootVisualElement(loadedSceneVisualElement)
                .InjectAllComponentsInChildren(loadedGameObject);
        }
        
        // Update translations
        foreach (GameObject loadedGameObject in loadedGameObjects)
        {
            loadedGameObject.GetComponentsInChildren<ITranslator>()
                .ForEach(it => it.UpdateTranslation());
        }

        // Apply theme to loaded UI
        ThemeManager.ApplyThemeSpecificStylesToVisualElementsInScene();
    }

    public void UnloadScene()
    {
        Debug.Log($"Unloading scene {GetCurrentScene()}");
        GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject loadedGameObject in rootGameObjects)
        {
            if (ShouldKeepLoadedGameObject(loadedGameObject))
            {
                continue;
            }
            Destroy(loadedGameObject);
        }
        loadedSceneRecipe = null;
    }

    private bool ShouldKeepLoadedGameObject(GameObject loadedGameObject)
    {
        return loadedGameObject.GetComponent<UIDocument>() != null
            || loadedGameObject.GetComponentInChildren<CommonSceneObjectsBinder>() != null;
    }

    public EScene GetCurrentScene()
    {
        if (loadedSceneRecipe != null)
        {
            // Something else may have been loaded into the original scene. 
            return loadedSceneRecipe.scene;
        }
        
        return ESceneUtils.GetSceneByBuildIndex(SceneManager.GetActiveScene().buildIndex);
    }
}
