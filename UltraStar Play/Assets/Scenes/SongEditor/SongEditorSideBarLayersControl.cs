using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class SongEditorSideBarLayersControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(songEditorLayerSideBarEntryUi))]
    private VisualTreeAsset songEditorLayerSideBarEntryUi;

    [Inject(UxmlName = R.UxmlNames.layersSideBarContainer)]
    private VisualElement layersSideBarContainer;

    [Inject]
    private Injector injector;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongEditorLayerManager layerManager;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private SongEditorSelectionControl selectionControl;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    private readonly List<SongEditorSideBarLayerEntryControl> layerEntryControls = new();

    public void OnInjectionFinished()
    {
        layerManager.GetVoiceLayers()
            .ForEach(layer => CreateLayerInputControl(layer));
        layerManager.GetEnumLayers()
            .Where(it => it.LayerEnum != ESongEditorLayer.CopyPaste)
            .ForEach(layer => CreateLayerInputControl(layer));
    }

    private void CreateLayerInputControl(AbstractSongEditorLayer layer)
    {
        VisualElement visualElement = songEditorLayerSideBarEntryUi.CloneTree().Children().First();
        layersSideBarContainer.Add(visualElement);

        SongEditorSideBarLayerEntryControl songEditorSideBarLayerEntryControl = injector
            .WithRootVisualElement(visualElement)
            .WithBindingForInstance(layer)
            .CreateAndInject<SongEditorSideBarLayerEntryControl>();

        layerEntryControls.Add(songEditorSideBarLayerEntryControl);
    }
}
