using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VfxManager : AbstractSingletonBehaviour, INeedInjection
{
    public static VfxManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<VfxManager>();

    [InjectedInInspector]
    public Camera foregroundVfxCamera;
    
    [InjectedInInspector]
    public Camera backgroundVfxCamera;
    
    [InjectedInInspector]
    public int foregroundVfxLayer = 1;

    [InjectedInInspector]
    public int backgroundVfxLayer = 1;
    
    [InjectedInInspector]
    public EParticleEffect testParticleEffect = EParticleEffect.FireworkCyanPurple;
    
    [InjectedInInspector]
    public float testParticleScale = 0.5f;

    [InjectedInInspector]
    public bool testParticleIsBackground = false;

    [InjectedInInspector]
    public List<ParticleEffectRecipe> particleRecipes;

    [Inject]
    private UIDocument uiDocument;
    
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private PanelHelper panelHelper;
    
    private Image foregroundVfxElement;
    private Image backgroundVfxElement;

    private Dictionary<EParticleEffect, GameObject> particleEffectToPrefabMap;

    private RenderTexture foregroundVfxRenderTexture;
    public RenderTexture ForegroundVfxRenderTexture
    {
        get
        {
            if (foregroundVfxRenderTexture != null
                && (foregroundVfxRenderTexture.width != Screen.width
                    || foregroundVfxRenderTexture.height != Screen.height))
            {
                Debug.Log("Recreate foregroundVfxRenderTexture because of screen size change.");
                Destroy(foregroundVfxRenderTexture);
                foregroundVfxRenderTexture = null;
            }
            
            if (foregroundVfxRenderTexture == null)
            {
                foregroundVfxRenderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            }

            return foregroundVfxRenderTexture;
        }
    }
    
    private RenderTexture backgroundVfxRenderTexture;
    public RenderTexture BackgroundVfxRenderTexture
    {
        get
        {
            if (backgroundVfxRenderTexture != null
                && (backgroundVfxRenderTexture.width != Screen.width
                    || backgroundVfxRenderTexture.height != Screen.height))
            {
                Debug.Log("Recreate foregroundVfxRenderTexture because of screen size change.");
                Destroy(backgroundVfxRenderTexture);
                backgroundVfxRenderTexture = null;
            }
            
            if (backgroundVfxRenderTexture == null)
            {
                backgroundVfxRenderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            }

            return backgroundVfxRenderTexture;
        }
    }
    
    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        Init();
        sceneNavigator.BeforeSceneChangeEventStream.Subscribe(_ => RemoveParticleSystems());
        sceneNavigator.SceneChangedEventStream.Subscribe(_ => Init());
    }

    private void RemoveParticleSystems()
    {
        foreach (Transform child in foregroundVfxCamera.transform)
        {
            Destroy(child.gameObject);
        }
        
        foreach (Transform child in backgroundVfxCamera.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void Init()
    {
        InitCameraTargetTextures();
        InitParticleEffectToPrefabMap();
        InitVfxElement();
    }
    
    private void InitCameraTargetTextures()
    {
        foregroundVfxCamera.targetTexture = ForegroundVfxRenderTexture;
        backgroundVfxCamera.targetTexture = BackgroundVfxRenderTexture;
    }

    private void Update()
    {
        if (Keyboard.current.rightCtrlKey.wasPressedThisFrame)
        {
            CreateParticleSystem(testParticleEffect, new Vector2(400, 300), testParticleScale, false, testParticleIsBackground);
        }
    }

    private void InitVfxElement()
    {
        foregroundVfxElement = uiDocument.rootVisualElement.Q<Image>("foregroundVfxElement");
        if (foregroundVfxElement != null)
        {
            return;
        }

        foregroundVfxElement = new Image();
        foregroundVfxElement.name = "foregroundVfxElement";
        foregroundVfxElement.AddToClassList("overlay");
        foregroundVfxElement.image = foregroundVfxCamera.targetTexture;
        foregroundVfxElement.pickingMode = PickingMode.Ignore;
        uiDocument.rootVisualElement.Add(foregroundVfxElement);
        
        backgroundVfxElement = new Image();
        backgroundVfxElement.name = "foregroundVfxElement";
        backgroundVfxElement.AddToClassList("overlay");
        backgroundVfxElement.image = backgroundVfxCamera.targetTexture;
        backgroundVfxElement.pickingMode = PickingMode.Ignore;
        uiDocument.rootVisualElement.Q(R.UxmlNames.background).AddAsFirstChild(backgroundVfxElement);
    }

    public static GameObject CreateParticleSystem(EParticleEffect particleEffect, Vector2 panelPos, float scale = 0, bool loop = false, bool isBackground = false)
    {
        VfxManager vfxManager = Instance;
        if (vfxManager == null)
        {
            return null;
        }
        return vfxManager.DoCreateParticleSystem(particleEffect, panelPos, scale, loop, isBackground);
    }
    
    private GameObject DoCreateParticleSystem(EParticleEffect particleEffect,
        Vector2 panelPos,
        float scale = 0,
        bool loop = false,
        bool isBackground = false)
    {
        if (!particleEffectToPrefabMap.TryGetValue(particleEffect, out GameObject particleSystemPrefab)
            || particleSystemPrefab == null)
        {
            return null;
        }
        
        Vector2 screenPos = panelHelper.PanelToScreen(panelPos);
        Vector3 worldPoint = foregroundVfxCamera.ScreenToWorldPoint(screenPos.WithZ(10f));
        
        Transform newParent = isBackground
            ? backgroundVfxCamera.transform
            : foregroundVfxCamera.transform;
        GameObject particleSystemInstance = Instantiate(particleSystemPrefab.gameObject,
            worldPoint,
            Quaternion.identity,
            newParent);

        particleSystemInstance.GetComponentsInChildren<ParticleSystem>().ForEach(particleSystem =>
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = loop;

            if (scale > 0)
            {
                // Scale with parents
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                // Scale gravity
                main.gravityModifierMultiplier *= scale;
            }
        });
        if (scale > 0)
        {
            particleSystemInstance.transform.localScale = new Vector3(scale, scale, scale);
        }

        // Set the layer for all instantiated objects
        int vfxLayer = isBackground
            ? backgroundVfxLayer
            : foregroundVfxLayer;
        particleSystemInstance.GetComponentsInChildren<Transform>()
            .ForEach(child => child.gameObject.layer = vfxLayer);

        return particleSystemInstance;
    }

    private void InitParticleEffectToPrefabMap()
    {
        particleEffectToPrefabMap = new();
        particleRecipes.ForEach(particleRecipe =>
            particleEffectToPrefabMap[particleRecipe.effectEnum] = particleRecipe.prefab);
    }

    protected override void OnDestroySingleton()
    {
        Destroy(foregroundVfxRenderTexture);
    }
}
