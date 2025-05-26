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
    private SongMetaChangedEventStream songMetaChangedEventStream;

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
        // LayerManager might not be initialized yet. So wait one frame.
        AwaitableUtils.ExecuteAfterDelayInFramesAsync(1, () =>
        {
            layerManager.GetVoiceLayers()
                .ForEach(layer => CreateLayerInputControl(layer));
            layerManager.GetEnumLayers()
                .ForEach(layer => CreateLayerInputControl(layer));
        });
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
