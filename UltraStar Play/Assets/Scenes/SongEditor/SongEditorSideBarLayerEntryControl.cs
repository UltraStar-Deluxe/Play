using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class SongEditorSideBarLayerEntryControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongEditorLayer layer;

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
        layerColorElement.style.backgroundColor = layerManager.GetColor(layer.LayerEnum);
        layerNameLabel.text = layer.LayerEnum.ToString();
        selectAllNotesOfLayerButton.RegisterCallbackButtonTriggered(
            () => selectionControl.SetSelection(layerManager.GetNotes(layer.LayerEnum)));

        layerVisibleToggleButtonControl = new ToogleButtonControl(layerVisibleButton,
            layerVisibleButton.Q<VisualElement>(R.UxmlNames.layerVisibleIcon),
            layerVisibleButton.Q<VisualElement>(R.UxmlNames.layerInvisibleIcon),
            layerManager.IsLayerEnabled(layer.LayerEnum));
        layerVisibleToggleButtonControl.ValueChangedEventStream
            .Subscribe(evt => layerManager.SetLayerEnabled(layer.LayerEnum, evt.NewValue));

        layerEditableToggleButtonControl = new ToogleButtonControl(layerEditableButton,
            layerEditableButton.Q<VisualElement>(R.UxmlNames.layerEditableIcon),
            layerEditableButton.Q<VisualElement>(R.UxmlNames.layerNotEditableIcon),
            layerManager.IsLayerEditable(layer.LayerEnum));
        layerEditableToggleButtonControl.ValueChangedEventStream
            .Subscribe(evt => layerManager.SetLayerEditable(layer.LayerEnum, evt.NewValue));
    }

    public void UpdateInputControls()
    {
        layerVisibleToggleButtonControl.IsOn = layerManager.IsLayerEnabled(layer.LayerEnum);
        layerEditableToggleButtonControl.IsOn = layerManager.IsLayerEditable(layer.LayerEnum);
    }
}
