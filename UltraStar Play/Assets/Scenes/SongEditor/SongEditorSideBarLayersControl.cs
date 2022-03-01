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

    private readonly List<EditorLayerInputControl> editorLayerInputControls = new List<EditorLayerInputControl>();

    public void OnInjectionFinished()
    {
        CreateVoiceVisibleInputControl("P1");
        CreateVoiceVisibleInputControl("P2");

        layerManager.GetLayers()
            .Where(it => it.LayerEnum != ESongEditorLayer.CopyPaste)
            .ForEach(layer => CreateLayerInputControl(layer));
        layerManager.LayerChangedEventStream.Subscribe(_ => UpdateLayerInputControls());
    }

    private void UpdateLayerInputControls()
    {
        editorLayerInputControls.ForEach(editorLayerInputControl =>
        {
            editorLayerInputControl.Toggle.value = layerManager.IsLayerEnabled(editorLayerInputControl.Layer.LayerEnum);
        });
    }

    private void CreateLayerInputControl(SongEditorLayer layer)
    {
        VisualElement visualElement = songEditorLayerSideBarEntryUi.CloneTree().Children().First();
        layersSideBarContainer.Add(visualElement);

        visualElement.Q<VisualElement>(R.UxmlNames.layerColor).style.backgroundColor = layerManager.GetColor(layer.LayerEnum);
        visualElement.Q<Label>(R.UxmlNames.layerNameLabel).text = layer.LayerEnum.ToString();
        visualElement.Q<Button>(R.UxmlNames.selectAllNotesOfLayerButton).RegisterCallbackButtonTriggered(() =>
        {
            selectionControl.SetSelection(layerManager.GetNotes(layer.LayerEnum));
        });
        Toggle toggle = visualElement.Q<Toggle>(R.UxmlNames.layerEnabledToggle);
        toggle.value = layerManager.IsLayerEnabled(layer.LayerEnum);
        toggle.RegisterValueChangedCallback(evt =>
        {
            if (layerManager.IsLayerEnabled(layer.LayerEnum) != evt.newValue)
            {
                layerManager.SetLayerEnabled(layer.LayerEnum, evt.newValue);
            }
        });

        editorLayerInputControls.Add(new EditorLayerInputControl
        {
            Toggle = toggle,
            Layer = layer
        });
    }

    private void CreateVoiceVisibleInputControl(string voiceName)
    {
        bool isHidden = settings.SongEditorSettings.HideVoices.Contains(voiceName);

        VisualElement visualElement = songEditorLayerSideBarEntryUi.CloneTree().Children().First();
        layersSideBarContainer.Add(visualElement);

        visualElement.Q<VisualElement>(R.UxmlNames.layerColor).style.backgroundColor = songEditorSceneControl.GetColorForVoiceName(voiceName);
        visualElement.Q<Label>(R.UxmlNames.layerNameLabel).text = voiceName.Replace("P", "Player ");
        visualElement.Q<Button>(R.UxmlNames.selectAllNotesOfLayerButton).RegisterCallbackButtonTriggered(() =>
        {
            Voice voice = songMeta.GetVoice(voiceName);
            if (voice != null)
            {
                selectionControl.SetSelection(SongMetaUtils.GetAllNotes(voice));
            }
        });
        Toggle toggle = visualElement.Q<Toggle>(R.UxmlNames.layerEnabledToggle);
        toggle.value = !isHidden;
        toggle.RegisterValueChangedCallback(evt => OnVoiceVisibleToggleChanged(voiceName, evt.newValue));
    }

    private void OnVoiceVisibleToggleChanged(string voiceName, bool isVisible)
    {
        if (isVisible)
        {
            settings.SongEditorSettings.HideVoices.Remove(voiceName);
        }
        else
        {
            settings.SongEditorSettings.HideVoices.AddIfNotContains(voiceName);
        }
        editorNoteDisplayer.UpdateNotesAndSentences();
    }

    private class EditorLayerInputControl
    {
        public Toggle Toggle { get; set; }
        public SongEditorLayer Layer { get; set; }
    }
}
