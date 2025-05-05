using System;
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
    public static VfxManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<VfxManager>();

    public const string BackgroundVfxRenderTextureName = "VfxManager.BackgroundVfxRenderTexture";
    public const string ForegroundVfxRenderTextureName = "VfxManager.ForegroundVfxRenderTexture";

    [InjectedInInspector]
    public Camera foregroundVfxCamera;

    [InjectedInInspector]
    public Camera backgroundVfxCamera;

    // Tip: with a z coordinate near the rendered canvas,
    // the scene view camera in isometric mode renders similar to the game view.
    // Can be useful to check particle effects in the scene view (e.g. the shape of the particle system).
    [InjectedInInspector]
    public float particleZ = 19.99f;

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

    [Inject]
    private Settings settings;

    [Inject]
    private RenderTextureManager renderTextureManager;

    private Image foregroundVfxElement;
    private Image backgroundVfxElement;

    private Dictionary<EParticleEffect, GameObject> particleEffectToPrefabMap;

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
        renderTextureManager.GetOrCreateScreenAspectRatioRenderTexture(ForegroundVfxRenderTextureName,
            foregroundRenderTexture => foregroundVfxCamera.targetTexture = foregroundRenderTexture);
        renderTextureManager.GetOrCreateScreenAspectRatioRenderTexture(BackgroundVfxRenderTextureName,
                backgroundRenderTexture => backgroundVfxCamera.targetTexture = backgroundRenderTexture);
    }

    private void Update()
    {
        // Performance: Disable cameras when there are not particles to render.
        UpdateVfxCameraActive(foregroundVfxCamera);
        UpdateVfxCameraActive(backgroundVfxCamera);

#if UNITY_EDITOR
        if (Application.isEditor
            && Keyboard.current.rightCtrlKey.wasPressedThisFrame)
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
#endif
    }

    private void UpdateVfxCameraActive(Camera cam)
    {
        if (cam.gameObject.activeSelf
            && cam.transform.childCount <= 0)
        {
            cam.gameObject.SetActive(false);
            RenderTextureUtils.Clear(cam.targetTexture);
        }
        else if (!cam.gameObject.activeSelf
                 && cam.transform.childCount > 0)
        {
            cam.gameObject.SetActive(true);
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
        backgroundVfxElement.name = "backgroundVfxElement";
        backgroundVfxElement.AddToClassList("overlay");
        backgroundVfxElement.image = backgroundVfxCamera.targetTexture;
        backgroundVfxElement.pickingMode = PickingMode.Ignore;
        uiDocument.rootVisualElement.AddAsFirstChild(backgroundVfxElement);
    }

    public static void CreateParticleEffect(ParticleEffectConfig particleEffectConfig)
    {
        VfxManager vfxManager = Instance;
        if (vfxManager == null)
        {
            return;
        }
        vfxManager.DoCreateParticleEffect(particleEffectConfig);
    }

    private void DoCreateParticleEffect(ParticleEffectConfig particleEffectConfig)
    {
        if (!settings.EnableVfx)
        {
            return;
        }

        if (!particleEffectToPrefabMap.TryGetValue(particleEffectConfig.particleEffect, out GameObject particleSystemPrefab)
            || particleSystemPrefab == null)
        {
            return;
        }

        Transform newParent = particleEffectConfig.isBackground
            ? backgroundVfxCamera.transform
            : foregroundVfxCamera.transform;
        GameObject particleSystemInstance = Instantiate(particleSystemPrefab.gameObject,
            GetWorldPos(particleEffectConfig.panelPos),
            Quaternion.identity,
            newParent);

        UpdateParticleEffectMainModule(particleSystemInstance,
            mainModule => mainModule.loop = particleEffectConfig.loop);

        UpdateParticleEffectLayer(particleEffectConfig, particleSystemInstance);

        Vector3 scale = new Vector3(particleEffectConfig.scale, particleEffectConfig.scale, particleEffectConfig.scale);
        UpdateParticleEffectScale(scale, particleSystemInstance);

        if (particleEffectConfig.maxParticles > 0)
        {
            UpdateParticleEffectMainModule(particleSystemInstance,
                mainModule => mainModule.maxParticles = particleEffectConfig.maxParticles);
        }

        if (particleEffectConfig.rateOverTime > 0)
        {
            UpdateParticleEffectEmissionModule(particleSystemInstance,
                emissionModule => emissionModule.rateOverTime = particleEffectConfig.rateOverTime);
        }

        if (particleEffectConfig.simulationSpeed > 0)
        {
            UpdateParticleEffectMainModule(particleSystemInstance,
                mainModule => mainModule.simulationSpeed = particleEffectConfig.simulationSpeed);
        }

        DoUpdateParticleSystemWithTarget(particleEffectConfig, particleSystemInstance);

        RegisterTargetCallbacks(particleEffectConfig, particleSystemInstance);
    }

    private void UpdateParticleEffectEmissionModule(GameObject particleSystemInstance, Action<ParticleSystem.EmissionModule> callback)
    {
        particleSystemInstance
            .GetComponentsInChildren<ParticleSystem>()
            .ForEach(localParticleSystem =>
            {
                callback(localParticleSystem.emission);
            });
    }

    private void UpdateParticleEffectMainModule(GameObject particleSystemInstance, Action<ParticleSystem.MainModule> callback)
    {
        particleSystemInstance
            .GetComponentsInChildren<ParticleSystem>()
            .ForEach(localParticleSystem =>
            {
                callback(localParticleSystem.main);
            });
    }

    private Vector3 GetWorldPos(Vector2 panelPos)
    {
        Vector2 screenPos = panelHelper.PanelToScreen(panelPos);
        Vector3 worldPos = foregroundVfxCamera.ScreenToWorldPoint(screenPos.WithZ(particleZ));
        return worldPos;
    }

    private void RegisterTargetCallbacks(ParticleEffectConfig particleEffectConfig, GameObject particleSystemInstance)
    {
        VisualElement target = particleEffectConfig.target;
        if (target == null)
        {
            return;
        }

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

            DoUpdateParticleSystemWithTarget(particleEffectConfig, particleSystemInstance);
        }

        if (particleEffectConfig.destroyWithTarget)
        {
            target.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        if (particleEffectConfig.hideAndShowWithTarget
            || particleEffectConfig.moveWithTargetPanelPosProducer != null
            || particleEffectConfig.scaleBoxShapeWithTargetFactor > 0)
        {
            target.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
    }

    private void DoUpdateParticleSystemWithTarget(ParticleEffectConfig particleEffectConfig, GameObject particleSystemInstance)
    {
        VisualElement target = particleEffectConfig.target;
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

        if (particleEffectConfig.scaleBoxShapeWithTargetFactor > 0)
        {
            UpdateParticleEffectBoxShape(particleEffectConfig, particleSystemInstance);
        }
    }

    private void UpdateParticleEffectBoxShape(ParticleEffectConfig particleEffectConfig, GameObject particleSystemInstance)
    {
        VisualElement target = particleEffectConfig.target;
        if (target == null)
        {
            return;
        }

        ParticleSystem particleSystemComponent = particleSystemInstance.GetComponent<ParticleSystem>();
        ParticleSystem.ShapeModule shape = particleSystemComponent.shape;
        shape.scale = particleEffectConfig.scaleBoxShapeWithTargetFactor * target.worldBound.size;
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

    private void UpdateParticleEffectScale(Vector3 scale, GameObject particleEffectInstance)
    {
        if (scale == Vector3.zero)
        {
            return;
        }

        particleEffectInstance
            .GetComponentsInChildren<ParticleSystem>()
            .ForEach(localParticleSystem =>
            {
                ParticleSystem.MainModule main = localParticleSystem.main;
                // Scale with parents
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                // Scale gravity
                main.gravityModifierMultiplier *= scale.magnitude;
            });
        particleEffectInstance.transform.localScale = scale;
    }

    private void InitParticleEffectToPrefabMap()
    {
        particleEffectToPrefabMap = new();
        particleRecipes.ForEach(particleRecipe =>
            particleEffectToPrefabMap[particleRecipe.effectEnum] = particleRecipe.prefab);
    }
}
