using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

    private readonly List<SongPropertyInputControl> songPropertyInputControls = new();

    public void OnInjectionFinished()
    {
        CreateSongPropertiesInputControls();
        songMetaChangeEventStream
            .Where(evt => evt is SongPropertyChangedEvent)
            .Subscribe(_ => UpdateSongPropertyInputControls());

        detectBpmControl = new DetectBpmControl(detectBpmButton, detectBpmLabel);
        bpmTextField.AddToClassList("disabled");
        bpmTextField.value = songMeta.BeatsPerMinute.ToString("0.00", CultureInfo.InvariantCulture);

        string enterBpmMessage = "Enter new BPM value.\n" +
                         "For better accuracy, the BPM of the audio should at least be doubled.";
        setBpmChangeNoteDurationButton.RegisterCallbackButtonTriggered(_ =>
            songEditorSceneControl.CreateNumberInputDialog("Set BPM and change note duration", enterBpmMessage, newBpm => applyBpmDontAdjustNoteLengthAction.ExecuteAndNotify(newBpm)));
        setBpmKeepNoteDurationButton.RegisterCallbackButtonTriggered(_ =>
            songEditorSceneControl.CreateNumberInputDialog("Set BPM but keep note duration", enterBpmMessage, newBpm => applyBpmAndAdjustNoteLengthAction.ExecuteAndNotify(newBpm)));
    }

    private void CreateSongPropertiesInputControls()
    {
        CreateSongPropertiesInputControl(ESongProperty.Artist,
            Translation.Get(R.Messages.songProperty_artist),
            () => songMeta.Artist,
            null);
        CreateSongPropertiesInputControl(ESongProperty.Title,
            Translation.Get(R.Messages.songProperty_title),
            () => songMeta.Title,
            null);
        CreateSongPropertiesInputControl(ESongProperty.Mp3,
            "Audio",
            () => songMeta.Audio,
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
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.GapInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.GapInMillis = newValue));
        CreateSongPropertiesInputControl(ESongProperty.VideoGap,
            "Video Gap (ms)",
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.VideoGapInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.VideoGapInMillis = newValue));
        CreateSongPropertiesInputControl(ESongProperty.Start,
            "Skip Intro (ms, #START)",
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.StartInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.StartInMillis = (int)newValue));
        CreateSongPropertiesInputControl(ESongProperty.End,
            "Skip Outro (ms, #END)",
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.EndInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.EndInMillis = (int)newValue));
        CreateSongPropertiesInputControl(ESongProperty.PreviewStart,
            "Preview Start (ms)",
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.PreviewStartInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.PreviewStartInMillis = newValue));
        CreateSongPropertiesInputControl(ESongProperty.PreviewEnd,
            "Preview End (ms)",
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.PreviewEndInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.PreviewEndInMillis = newValue));
        CreateSongPropertiesInputControl(ESongProperty.MedleyStart,
            "Medley Start (ms)",
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.MedleyStartInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.MedleyStartInMillis = (int)newValue));
        CreateSongPropertiesInputControl(ESongProperty.MedleyEnd,
            "Medley End (ms)",
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.MedleyEndInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.MedleyEndInMillis = (int)newValue));
        CreateSongPropertiesInputControl(ESongProperty.Language,
            Translation.Get(R.Messages.songProperty_language),
            () => songMeta.Language,
            (newValue) => songMeta.Language = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Edition,
            Translation.Get(R.Messages.songProperty_edition),
            () => songMeta.Edition,
            (newValue) => songMeta.Edition = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Genre,
            Translation.Get(R.Messages.songProperty_genre),
            () => songMeta.Genre,
            (newValue) => songMeta.Genre = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Year,
            Translation.Get(R.Messages.songProperty_year),
            PropertyUtils.CreateStringGetterFromUintGetter(() => songMeta.Year, true),
            PropertyUtils.CreateStringSetterFromUintSetter(newValue => songMeta.Year = newValue));
        CreateSongPropertiesInputControl(ESongProperty.VocalsAudio,
            "Vocals Audio",
            () => songMeta.VocalsAudio,
            newValue => songMeta.VocalsAudio = newValue);
        CreateSongPropertiesInputControl(ESongProperty.InstrumentalAudio,
            "Instrumental Audio",
            () => songMeta.InstrumentalAudio,
            newValue => songMeta.InstrumentalAudio = newValue);

        songMeta.AdditionalHeaderEntries.ForEach(entry =>
        {
            CreateSongPropertiesInputControl(ESongProperty.Other,
                entry.Key,
                () => songMeta.AdditionalHeaderEntries[entry.Key],
                newValue => songMeta.SetAdditionalHeaderEntry(entry.Key, newValue));
        });
    }

    private void CreateSongPropertiesInputControl(ESongProperty songProperty, string labelText, Func<string> valueGetter, Action<string> valueSetter)
    {
        VisualElement visualElement = songPropertySideBarEntryUi.CloneTree().Children().First();
        songPropertiesSideBarContainer.Add(visualElement);

        TextField textField = visualElement.Q<TextField>(R.UxmlNames.propertyTextField);
        textField.DisableParseEscapeSequences();
        textField.label = labelText;
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

        bpmTextField.value = songMeta.BeatsPerMinute.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private class SongPropertyInputControl
    {
        public TextField TextField { get; set; }
        public string LabelText { get; set; }
        public Func<string> ValueGetter { get; set; }
        public Action<string> ValueSetter { get; set; }
    }
}
