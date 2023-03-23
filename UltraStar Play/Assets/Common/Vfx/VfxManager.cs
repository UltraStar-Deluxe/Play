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
            CreateParticleEffect(new ParticleEffectConfig()
            {
                particleEffect = testParticleEffect,
                panelPos = new Vector2(400, 300),
                scale = testParticleScale,
                loop = false,
                isBackground = testParticleIsBackground
            });
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

    public static GameObject CreateParticleEffect(ParticleEffectConfig particleEffectConfig)
    {
        VfxManager vfxManager = Instance;
        if (vfxManager == null)
        {
            return null;
        }
        return vfxManager.DoCreateParticleEffect(particleEffectConfig);
    }
    
    private GameObject DoCreateParticleEffect(ParticleEffectConfig particleEffectConfig)
    {
        if (!particleEffectToPrefabMap.TryGetValue(particleEffectConfig.particleEffect, out GameObject particleSystemPrefab)
            || particleSystemPrefab == null)
        {
            return null;
        }
        
        Transform newParent = particleEffectConfig.isBackground
            ? backgroundVfxCamera.transform
            : foregroundVfxCamera.transform;
        GameObject particleSystemInstance = Instantiate(particleSystemPrefab.gameObject,
            GetWorldPos(particleEffectConfig.panelPos),
            Quaternion.identity,
            newParent);

        UpdateParticleEffectLoop(particleEffectConfig, particleSystemInstance);
        UpdateParticleEffectScale(particleEffectConfig.scale, particleSystemInstance);
        UpdateParticleEffectLayer(particleEffectConfig, particleSystemInstance);

        RegisterTargetCallbacks(particleEffectConfig, particleSystemInstance);
        
        return particleSystemInstance;
    }

    private Vector3 GetWorldPos(Vector2 panelPos)
    {
        Vector2 screenPos = panelHelper.PanelToScreen(panelPos);
        Vector3 worldPos = foregroundVfxCamera.ScreenToWorldPoint(screenPos.WithZ(10f));
        return worldPos;
    }
    
    private void RegisterTargetCallbacks(ParticleEffectConfig particleEffectConfig, GameObject particleSystemInstance)
    {
        VisualElement target = particleEffectConfig.target;
        if (target == null)
        {
            return;
        }

        bool scaleWithTarget = particleEffectConfig.referenceTargetSize.x > 0
                               && particleEffectConfig.referenceTargetSize.y > 0 
                               && particleEffectConfig.referenceParticleSystemScale.x > 0
                               && particleEffectConfig.referenceParticleSystemScale.y > 0;

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (particleSystemInstance == null)
            {
                target.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
                return;
            }
            
            Destroy(particleSystemInstance);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (particleSystemInstance == null)
            {
                target.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                return;
            }
            
            if (!VisualElementUtils.HasGeometry(target))
            {
                return;
            }
            
            if (particleEffectConfig.hideAndShowWithTarget)
            {
                bool isTargetVisible = target.IsVisibleByDisplay()
                                       && target.IsVisibleByVisibility()
                                       && target.resolvedStyle.width > 0
                                       && target.resolvedStyle.height > 0;
                particleSystemInstance.SetActive(isTargetVisible);
            }
            
            if (particleEffectConfig.moveWithTargetPanelPosProducer != null)
            {
                Vector2 panelPos = particleEffectConfig.moveWithTargetPanelPosProducer();
                particleSystemInstance.transform.position = GetWorldPos(panelPos);
            }

            if (scaleWithTarget)
            {
                float targetScaleX = target.worldBound.width / particleEffectConfig.referenceTargetSize.x;
                // float targetScaleY = target.worldBound.height / particleEffectConfig.referenceTargetSize.y;
                float particleScaleX = particleEffectConfig.referenceTargetSize.x * targetScaleX;
                // float particleScaleY = particleEffectConfig.referenceTargetSize.y * targetScaleY;
                
                UpdateParticleEffectScale(particleScaleX, particleSystemInstance);
            }
        }
        
        if (particleEffectConfig.destroyWithTarget)
        {
            target.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        if (particleEffectConfig.hideAndShowWithTarget
            || particleEffectConfig.moveWithTargetPanelPosProducer != null
            || scaleWithTarget)
        {
            target.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
    }
    
    private void UpdateParticleEffectLoop(ParticleEffectConfig particleEffectConfig, GameObject particleSystemInstance)
    {
        particleSystemInstance
            .GetComponentsInChildren<ParticleSystem>()
            .ForEach(particleSystem =>
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.loop = particleEffectConfig.loop;
            });
    }

    private void UpdateParticleEffectLayer(ParticleEffectConfig particleEffectConfig, GameObject particleSystemInstance)
    {
        // Set the layer for all instantiated objects
        int vfxLayer = particleEffectConfig.isBackground
            ? backgroundVfxLayer
            : foregroundVfxLayer;
        particleSystemInstance
            .GetComponentsInChildren<Transform>()
            .ForEach(child => child.gameObject.layer = vfxLayer);
    }

    private void UpdateParticleEffectScale(float scale, GameObject particleEffectInstance)
    {
        if (scale <= 0)
        {
            return;
        }

        particleEffectInstance
            .GetComponentsInChildren<ParticleSystem>()
            .ForEach(particleSystem =>
            {
                ParticleSystem.MainModule main = particleSystem.main;
                // Scale with parents
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                // Scale gravity
                main.gravityModifierMultiplier *= scale;
            });
        particleEffectInstance.transform.localScale = new Vector3(scale, scale, scale);
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
