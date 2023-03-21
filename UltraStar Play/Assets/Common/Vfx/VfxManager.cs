using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VfxManager : AbstractSingletonBehaviour, INeedInjection
{
    public VfxManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<VfxManager>();

    [InjectedInInspector]
    public string targetVisualElementName;

    [InjectedInInspector]
    public GameObject particleSystemPrefab;
    
    [InjectedInInspector]
    public Camera vfxCamera;
    
    [InjectedInInspector]
    public int vfxLayer = 1;
    
    [Inject]
    private UIDocument uiDocument;
    
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private PanelHelper panelHelper;
    
    private Image vfxElement;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        InitVfxElement();
        sceneNavigator.SceneChangedEventStream.Subscribe(_ => InitVfxElement());
    }

    private void InitVfxElement()
    {
        vfxElement = uiDocument.rootVisualElement.Q<Image>("VfxOverlay");
        if (vfxElement != null)
        {
            return;
        }

        vfxElement = new Image();
        vfxElement.name = "vfxOverlay";
        vfxElement.image = vfxCamera.targetTexture;
        uiDocument.rootVisualElement.Add(vfxElement);
    }

    public void Update()
    {
        if (Keyboard.current.leftAltKey.wasPressedThisFrame)
        {
            Debug.Log("Creating particle system.");
            
            VisualElement targetVisualElement = uiDocument.rootVisualElement.Q(targetVisualElementName);
            if (targetVisualElement == null)
            {
                Debug.LogWarning("Target element not found.");
                return;
            }
            CreateParticleSystem(targetVisualElement);
        }
    }

    public void CreateParticleSystem(Vector2 panelPos, float scale = 0)
    {
        Vector2 screenPos = panelHelper.PanelToScreen(panelPos);
        Vector3 worldPoint = vfxCamera.ScreenToWorldPoint(screenPos.WithZ(10f));
        Debug.Log($"panelPos: {panelPos}, screenPos: {screenPos}, screenSize: {ApplicationUtils.GetScreenSize()}, targetWorldPoint: {worldPoint}");
        
        GameObject particleSystemInstance = Instantiate(particleSystemPrefab.gameObject,
            worldPoint,
            Quaternion.identity,
            vfxCamera.transform);

        if (scale > 0)
        {
            particleSystemInstance.transform.localScale = new Vector3(scale, scale, scale);
        }
        
        // Set the layer for all instantiated objects
        particleSystemInstance.GetComponentsInChildren<Transform>()
            .ForEach(child => child.gameObject.layer = vfxLayer);
    }
    
    public void CreateParticleSystem(VisualElement target)
    {
        CreateParticleSystem(target.worldBound.center);
    }
}
