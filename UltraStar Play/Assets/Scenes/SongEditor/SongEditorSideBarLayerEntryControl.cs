using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class SongEditorSideBarLayerEntryControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private AbstractSongEditorLayer layer;

    [Inject]
    private SongEditorLayerManager layerManager;

    [Inject]
    private SongEditorSelectionControl selectionControl;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement visualElement;

    [Inject(UxmlName = R.UxmlNames.layerColor)]
    private VisualElement layerColorElement;

    [Inject(UxmlName = R.UxmlNames.layerNameLabel)]
    private Label layerNameLabel;

    [Inject(UxmlName = R.UxmlNames.selectAllNotesOfLayerButton)]
    private Button selectAllNotesOfLayerButton;

    [Inject(UxmlName = R.UxmlNames.layerVisibleButton)]
    private Button layerVisibleButton;

    [Inject(UxmlName = R.UxmlNames.layerEditableButton)]
    private Button layerEditableButton;

    private ToogleButtonControl layerVisibleToggleButtonControl;
    private ToogleButtonControl layerEditableToggleButtonControl;

    public void OnInjectionFinished()
    {
        layerColorElement.style.backgroundColor = layerManager.GetLayerColor(layer);
        layerNameLabel.text = layer.GetDisplayName();
        selectAllNotesOfLayerButton.RegisterCallbackButtonTriggered(
            _ => selectionControl.SetSelection(layerManager.GetLayerNotes(layer)));

        // IsVisible
        layerVisibleToggleButtonControl = new ToogleButtonControl(layerVisibleButton,
            layerVisibleButton.Q<VisualElement>(R.UxmlNames.layerVisibleIcon),
            layerVisibleButton.Q<VisualElement>(R.UxmlNames.layerInvisibleIcon),
            layerManager.IsLayerVisible(layer));
        layerVisibleToggleButtonControl.ValueChangedEventStream
            .Subscribe(evt => layerManager.SetLayerVisible(layer, evt.NewValue));

        // IsEditable
        layerEditableToggleButtonControl = new ToogleButtonControl(layerEditableButton,
            layerEditableButton.Q<VisualElement>(R.UxmlNames.layerEditableIcon),
            layerEditableButton.Q<VisualElement>(R.UxmlNames.layerNotEditableIcon),
            layerManager.IsLayerEditable(layer));
        layerEditableToggleButtonControl.ValueChangedEventStream
            .Subscribe(evt => layerManager.SetLayerEditable(layer, evt.NewValue));
    }

    public void UpdateInputControls()
    {
        layerVisibleToggleButtonControl.IsOn = layerManager.IsLayerVisible(layer);
        layerEditableToggleButtonControl.IsOn = layerManager.IsLayerEditable(layer);
    }
}
