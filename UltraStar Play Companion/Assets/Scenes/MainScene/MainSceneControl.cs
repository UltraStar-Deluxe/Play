using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MainSceneControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener, IBinder
{
    [InjectedInInspector]
    public VisualTreeAsset playerSelectPlayerEntryUi;

    [InjectedInInspector]
    public TextAsset versionPropertiesTextAsset;

    [InjectedInInspector]
    public SongListRequestor songListRequestor;

    [InjectedInInspector]
    public ClientSideMicDataSender clientSideMicDataSender;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Settings settings;

    [Inject]
    private Injector injector;

    [Inject]
    private ClientSideCompanionClientManager clientSideCompanionClientManager;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.tabGroup)]
    private VisualElement tabGroup;

    private float frameCountTime;
    private int frameCount;

    private readonly BuildInfoUiControl buildInfoUiControl = new();
    private readonly ConnectionStatusUiControl connectionStatusUiControl = new();
    private readonly InputSimulationControl inputSimulationControl = new();
    private readonly MainSceneTabGroupUiControl mainSceneTabGroupUiControl = new();
    private readonly MenuUiControl menuUiControl = new();
    private readonly MicrophoneUiControl microphoneUiControl = new();
    private readonly OnlyVisibleWhenControl onlyVisibleWhenControl = new();
    private readonly SongListControl songListControl = new();

    public void OnInjectionFinished()
    {
        injector
            .WithBindingForInstance(versionPropertiesTextAsset)
            .Inject(buildInfoUiControl);
        injector.Inject(connectionStatusUiControl);
        injector.Inject(inputSimulationControl);
        injector.Inject(mainSceneTabGroupUiControl);
        injector.Inject(menuUiControl);
        injector.Inject(microphoneUiControl);
        injector.Inject(onlyVisibleWhenControl);
        injector.Inject(songListControl);

        UpdateTranslation();
    }

    public void Update()
    {
        microphoneUiControl.Update();
    }

    private void UpdateTranslation()
    {
        sceneTitle.text = Translation.Get(R.Messages.companionApp_title);

        songListControl.UpdateTranslation();
    }

    private void LateUpdate()
    {
        songListControl?.LateUpdate();
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(clientSideMicDataSender);
        bb.BindExistingInstance(inputSimulationControl);
        bb.BindExistingInstance(songListRequestor);
        bb.BindExistingInstance(songListControl);
        bb.Bind(nameof(playerSelectPlayerEntryUi)).ToExistingInstance(playerSelectPlayerEntryUi);
        return bb.GetBindings();
    }

    public void OnDestroy()
    {
        songListControl.Dispose();
    }
}
