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

        Translation enterBpmMessage = Translation.Get(R.Messages.songEditor_setBpmDialog_message);
        setBpmChangeNoteDurationButton.RegisterCallbackButtonTriggered(_ =>
            songEditorSceneControl.CreateNumberInputDialog(Translation.Get(R.Messages.songEditor_setBpmChangeNoteDurationDialog_title), enterBpmMessage, newBpm => applyBpmDontAdjustNoteLengthAction.ExecuteAndNotify(newBpm)));
        setBpmKeepNoteDurationButton.RegisterCallbackButtonTriggered(_ =>
            songEditorSceneControl.CreateNumberInputDialog(Translation.Get(R.Messages.songEditor_setBpmKeepNoteDurationDialog_title), enterBpmMessage, newBpm => applyBpmAndAdjustNoteLengthAction.ExecuteAndNotify(newBpm)));
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
            Translation.Get(R.Messages.songEditor_songProperty_audio),
            () => songMeta.Audio,
            null);
        CreateSongPropertiesInputControl(ESongProperty.Video,
            Translation.Get(R.Messages.songEditor_songProperty_video),
            () => songMeta.Video,
            newValue => songMeta.Video = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Background,
            Translation.Get(R.Messages.songEditor_songProperty_background),
            () => songMeta.Background,
            newValue => songMeta.Background = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Cover,
            Translation.Get(R.Messages.songEditor_songProperty_cover),
            () => songMeta.Cover,
            newValue => songMeta.Cover = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Gap,
            Translation.Get(R.Messages.songEditor_songProperty_gap),
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.GapInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.GapInMillis = newValue));
        CreateSongPropertiesInputControl(ESongProperty.VideoGap,
            Translation.Get(R.Messages.songEditor_songProperty_videoGap),
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.VideoGapInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.VideoGapInMillis = newValue));
        CreateSongPropertiesInputControl(ESongProperty.Start,
            Translation.Get(R.Messages.songEditor_songProperty_start),
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.StartInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.StartInMillis = (int)newValue));
        CreateSongPropertiesInputControl(ESongProperty.End,
            Translation.Get(R.Messages.songEditor_songProperty_end),
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.EndInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.EndInMillis = (int)newValue));
        CreateSongPropertiesInputControl(ESongProperty.PreviewStart,
            Translation.Get(R.Messages.songEditor_songProperty_previewStart),
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.PreviewStartInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.PreviewStartInMillis = newValue));
        CreateSongPropertiesInputControl(ESongProperty.PreviewEnd,
            Translation.Get(R.Messages.songEditor_songProperty_previewEnd),
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.PreviewEndInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.PreviewEndInMillis = newValue));
        CreateSongPropertiesInputControl(ESongProperty.MedleyStart,
            Translation.Get(R.Messages.songEditor_songProperty_medleyStart),
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.MedleyStartInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.MedleyStartInMillis = (int)newValue));
        CreateSongPropertiesInputControl(ESongProperty.MedleyEnd,
            Translation.Get(R.Messages.songEditor_songProperty_medleyEnd),
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.MedleyEndInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.MedleyEndInMillis = (int)newValue));
        CreateSongPropertiesInputControl(ESongProperty.Language,
            Translation.Get(R.Messages.songEditor_songProperty_language),
            () => songMeta.Language,
            (newValue) => songMeta.Language = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Genre,
            Translation.Get(R.Messages.songEditor_songProperty_genre),
            () => songMeta.Genre,
            (newValue) => songMeta.Genre = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Tag,
            Translation.Get(R.Messages.songEditor_songProperty_tags),
            () => songMeta.Tag,
            (newValue) => songMeta.Tag = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Edition,
            Translation.Get(R.Messages.songEditor_songProperty_edition),
            () => songMeta.Edition,
            (newValue) => songMeta.Edition = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Year,
            Translation.Get(R.Messages.songEditor_songProperty_year),
            PropertyUtils.CreateStringGetterFromUintGetter(() => songMeta.Year, true),
            PropertyUtils.CreateStringSetterFromUintSetter(newValue => songMeta.Year = newValue));
        CreateSongPropertiesInputControl(ESongProperty.VocalsAudio,
            Translation.Get(R.Messages.songEditor_songProperty_vocalsAudio),
            () => songMeta.VocalsAudio,
            newValue => songMeta.VocalsAudio = newValue);
        CreateSongPropertiesInputControl(ESongProperty.InstrumentalAudio,
            Translation.Get(R.Messages.songEditor_songProperty_instrumentalAudio),
            () => songMeta.InstrumentalAudio,
            newValue => songMeta.InstrumentalAudio = newValue);

        songMeta.AdditionalHeaderEntries.ForEach(entry =>
        {
            CreateSongPropertiesInputControl(ESongProperty.Other,
                Translation.Of(entry.Key),
                () => songMeta.AdditionalHeaderEntries[entry.Key],
                newValue => songMeta.SetAdditionalHeaderEntry(entry.Key, newValue));
        });
    }

    private void CreateSongPropertiesInputControl(ESongProperty songProperty, Translation labelText, Func<string> valueGetter, Action<string> valueSetter)
    {
        VisualElement visualElement = songPropertySideBarEntryUi.CloneTree().Children().First();
        songPropertiesSideBarContainer.Add(visualElement);

        TextField textField = visualElement.Q<TextField>(R.UxmlNames.propertyTextField);
        textField.DisableParseEscapeSequences();
        textField.SetTranslatedLabel(labelText);
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
