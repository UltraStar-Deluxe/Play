using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class BackgroundLightManager : AbstractSingletonBehaviour, INeedInjection
{
    public static BackgroundLightManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<BackgroundLightManager>();

    [InjectedInInspector]
    public RenderTexture backgroundLightRenderTexture;
        
    [InjectedInInspector]
    public GameObject backgroundLightInstancesParent;

    [Inject]
    private Settings settings;

    public int BackgroundLightInstancesCount => backgroundLightInstancesParent.transform.childCount;

    private bool isBackgroundLightEnabled = true;
    public bool IsBackgroundLightEnabled
    {
        get => isBackgroundLightEnabled;
        set
        {
            isBackgroundLightEnabled = value;
            if (isBackgroundLightEnabled)
            {
                SetActiveBackgroundLight(settings.BackgroundLightIndex);
            }
            else
            {
                SetActiveBackgroundLight(0);
            }
        }
    }

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        settings
            .ObserveEveryValueChanged(it => it.BackgroundLightIndex)
            .Subscribe(newValue => SetActiveBackgroundLight(newValue));
    }

    private void SetActiveBackgroundLight(int index)
    {
        if (index <= 0)
        {
            RenderTextureUtils.Clear(backgroundLightRenderTexture);
            foreach (Transform child in backgroundLightInstancesParent.transform)
            {
                child.gameObject.SetActive(false);
            }
            return;
        }

        int iteration = 0;
        foreach (Transform child in backgroundLightInstancesParent.transform)
        {
            child.gameObject.SetActive(iteration == (index- 1));
            iteration++;
        }
    }
}
