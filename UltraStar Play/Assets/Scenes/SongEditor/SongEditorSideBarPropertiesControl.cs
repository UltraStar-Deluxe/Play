using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class SongEditorSideBarPropertiesControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(songPropertySideBarEntryUi))]
    private VisualTreeAsset songPropertySideBarEntryUi;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongMeta songMeta;

    [Inject(UxmlName = R.UxmlNames.songPropertiesSideBarContainer)]
    private VisualElement songPropertiesSideBarContainer;

    [Inject(UxmlName = R.UxmlNames.detectBpmButton)]
    private Button detectBpmButton;

    [Inject(UxmlName = R.UxmlNames.detectBpmLabel)]
    private Label detectBpmLabel;

    [Inject(UxmlName = R.UxmlNames.bpmTextField)]
    private TextField bpmTextField;

    [Inject(UxmlName = R.UxmlNames.setBpmKeepNoteDurationButton)]
    private Button setBpmKeepNoteDurationButton;

    [Inject(UxmlName = R.UxmlNames.setBpmChangeNoteDurationButton)]
    private Button setBpmChangeNoteDurationButton;

    [Inject]
    private ApplyBpmAndAdjustNoteLengthAction applyBpmAndAdjustNoteLengthAction;

    [Inject]
    private ApplyBpmDontAdjustNoteLengthAction applyBpmDontAdjustNoteLengthAction;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    private DetectBpmControl detectBpmControl;

    private readonly List<SongPropertyInputControl> songPropertyInputControls = new List<SongPropertyInputControl>();

    public void OnInjectionFinished()
    {
        CreateSongPropertiesInputControls();
        songMetaChangeEventStream
            .Where(evt => evt is SongPropertyChangedEvent)
            .Subscribe(_ => UpdateSongPropertyInputControls());

        detectBpmControl = new DetectBpmControl(detectBpmButton, detectBpmLabel);
        bpmTextField.AddToClassList("disabled");
        bpmTextField.value = songMeta.Bpm.ToString("0.00", CultureInfo.InvariantCulture);

        setBpmChangeNoteDurationButton.RegisterCallbackButtonTriggered(() =>
            songEditorSceneControl.CreateNumberInputDialog("Set BPM and change note duration", "Enter new BPM value", newBpm => applyBpmDontAdjustNoteLengthAction.ExecuteAndNotify(newBpm)));
        setBpmKeepNoteDurationButton.RegisterCallbackButtonTriggered(() =>
            songEditorSceneControl.CreateNumberInputDialog("Set BPM but keep note duration", "Enter new BPM value", newBpm => applyBpmAndAdjustNoteLengthAction.ExecuteAndNotify(newBpm)));
    }

    private void CreateSongPropertiesInputControls()
    {
        CreateSongPropertiesInputControl(ESongProperty.Artist,
            TranslationManager.GetTranslation(R.Messages.songProperty_artist),
            () => songMeta.Artist,
            null);
        CreateSongPropertiesInputControl(ESongProperty.Title,
            TranslationManager.GetTranslation(R.Messages.songProperty_title),
            () => songMeta.Title,
            null);
        CreateSongPropertiesInputControl(ESongProperty.Mp3,
            "Audio",
            () => songMeta.Mp3,
            null);
        CreateSongPropertiesInputControl(ESongProperty.Video,
            "Video",
            () => songMeta.Video,
            newValue => songMeta.Video = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Background,
            "Background",
            () => songMeta.Background,
            newValue => songMeta.Background = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Cover,
            "Cover",
            () => songMeta.Cover,
            newValue => songMeta.Cover = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Gap,
            "Gap (ms)",
            PropertyUtils.CreateStringGetterFromFloatGetter(() => songMeta.Gap, true, "0.00"),
            PropertyUtils.CreateStringSetterFromFloatSetter(newValue => songMeta.Gap = newValue));
        CreateSongPropertiesInputControl(ESongProperty.VideoGap,
            "Video Gap (s)",
            PropertyUtils.CreateStringGetterFromFloatGetter(() => songMeta.VideoGap, true, "0.00"),
            PropertyUtils.CreateStringSetterFromFloatSetter(newValue => songMeta.VideoGap = newValue));
        CreateSongPropertiesInputControl(ESongProperty.PreviewStart,
            "Preview Start (beat)",
            PropertyUtils.CreateStringGetterFromFloatGetter(() => songMeta.PreviewStart, true, "0.00"),
            PropertyUtils.CreateStringSetterFromFloatSetter(newValue => songMeta.PreviewStart = newValue));
        CreateSongPropertiesInputControl(ESongProperty.PreviewEnd,
            "Preview End (beat)",
            PropertyUtils.CreateStringGetterFromFloatGetter(() => songMeta.PreviewEnd, true, "0.00"),
            PropertyUtils.CreateStringSetterFromFloatSetter(newValue => songMeta.PreviewEnd = newValue));
        CreateSongPropertiesInputControl(ESongProperty.Language,
            TranslationManager.GetTranslation(R.Messages.songProperty_language),
            () => songMeta.Language,
            (newValue) => songMeta.Language = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Edition,
            TranslationManager.GetTranslation(R.Messages.songProperty_edition),
            () => songMeta.Edition,
            (newValue) => songMeta.Edition = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Genre,
            TranslationManager.GetTranslation(R.Messages.songProperty_genre),
            () => songMeta.Genre,
            (newValue) => songMeta.Genre = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Year,
            TranslationManager.GetTranslation(R.Messages.songProperty_year),
            PropertyUtils.CreateStringGetterFromUintGetter(() => songMeta.Year, true),
            PropertyUtils.CreateStringSetterFromUintSetter(newValue => songMeta.Year = newValue));

        songMeta.UnknownHeaderEntries.ForEach(entry =>
        {
            CreateSongPropertiesInputControl(ESongProperty.Other,
                entry.Key,
                () => songMeta.UnknownHeaderEntries[entry.Key],
                newValue => songMeta.SetUnknownHeaderEntry(entry.Key, newValue));
        });
    }

    private void CreateSongPropertiesInputControl(ESongProperty songProperty, string labelText, Func<string> valueGetter, Action<string> valueSetter)
    {
        VisualElement visualElement = songPropertySideBarEntryUi.CloneTree().Children().First();
        songPropertiesSideBarContainer.Add(visualElement);

        Label label = visualElement.Q<Label>(R.UxmlNames.propertyNameLabel);
        label.text = labelText;
        TextField textField = visualElement.Q<TextField>(R.UxmlNames.propertyTextField);
        textField.isDelayed = true;
        textField.value = valueGetter();
        bool isReadOnly = valueSetter == null;
        if (isReadOnly)
        {
            textField.AddToClassList("disabled");
            textField.RegisterValueChangedCallback(evt =>
            {
                // Reset to old value
                string newValue = evt.newValue.Trim();
                if (newValue != valueGetter())
                {
                    textField.value = valueGetter();
                }
            });
        }
        else
        {
            textField.RegisterValueChangedCallback(evt =>
            {
                string newValue = evt.newValue.Trim();
                if (newValue != valueGetter())
                {
                    valueSetter(newValue);
                    songMetaChangeEventStream.OnNext(new SongPropertyChangedEvent(songProperty));
                }
            });
        }

        songPropertyInputControls.Add(new SongPropertyInputControl
        {
            TextField = textField,
            Label = label,
            LabelText = labelText,
            ValueGetter = valueGetter,
            ValueSetter = valueSetter
        });
    }

    private void UpdateSongPropertyInputControls()
    {
        songPropertyInputControls.ForEach(it =>
        {
            string newValue = it.ValueGetter();
            if (newValue != it.TextField.value)
            {
                it.TextField.value = newValue;
            }
        });

        bpmTextField.value = songMeta.Bpm.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private class SongPropertyInputControl
    {
        public TextField TextField { get; set; }
        public Label Label { get; set; }
        public string LabelText { get; set; }
        public Func<string> ValueGetter { get; set; }
        public Action<string> ValueSetter { get; set; }
    }
}
