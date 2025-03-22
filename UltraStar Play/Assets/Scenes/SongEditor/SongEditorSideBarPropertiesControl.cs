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
    private SongMetaChangedEventStream songMetaChangedEventStream;

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
        songMetaChangedEventStream
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
            () => songMeta.Artist,
            null);
        CreateSongPropertiesInputControl(ESongProperty.Title,
            () => songMeta.Title,
            null);
        CreateSongPropertiesInputControl(ESongProperty.Mp3,
            () => songMeta.Audio,
            null);
        CreateSongPropertiesInputControl(ESongProperty.Video,
            () => songMeta.Video,
            newValue => songMeta.Video = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Background,
            () => songMeta.Background,
            newValue => songMeta.Background = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Cover,
            () => songMeta.Cover,
            newValue => songMeta.Cover = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Gap,
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.GapInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.GapInMillis = newValue),
            "ms");
        CreateSongPropertiesInputControl(ESongProperty.VideoGap,
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.VideoGapInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.VideoGapInMillis = newValue),
            "ms");
        CreateSongPropertiesInputControl(ESongProperty.Start,
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.StartInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.StartInMillis = (int)newValue),
            "ms");
        CreateSongPropertiesInputControl(ESongProperty.End,
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.EndInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.EndInMillis = (int)newValue),
            "ms");
        CreateSongPropertiesInputControl(ESongProperty.PreviewStart,
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.PreviewStartInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.PreviewStartInMillis = newValue),
            "ms");
        CreateSongPropertiesInputControl(ESongProperty.PreviewEnd,
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.PreviewEndInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.PreviewEndInMillis = newValue),
            "ms");
        CreateSongPropertiesInputControl(ESongProperty.MedleyStart,
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.MedleyStartInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.MedleyStartInMillis = (int)newValue),
            "ms");
        CreateSongPropertiesInputControl(ESongProperty.MedleyEnd,
            PropertyUtils.CreateStringGetterFromDoubleGetter(() => songMeta.MedleyEndInMillis, true, "0"),
            PropertyUtils.CreateStringSetterFromDoubleSetter(newValue => songMeta.MedleyEndInMillis = (int)newValue),
            "ms");
        CreateSongPropertiesInputControl(ESongProperty.Language,
            () => songMeta.Language,
            (newValue) => songMeta.Language = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Genre,
            () => songMeta.Genre,
            (newValue) => songMeta.Genre = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Tags,
            () => songMeta.Tag,
            (newValue) => songMeta.Tag = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Edition,
            () => songMeta.Edition,
            (newValue) => songMeta.Edition = newValue);
        CreateSongPropertiesInputControl(ESongProperty.Year,
            PropertyUtils.CreateStringGetterFromUintGetter(() => songMeta.Year, true),
            PropertyUtils.CreateStringSetterFromUintSetter(newValue => songMeta.Year = newValue));
        CreateSongPropertiesInputControl(ESongProperty.VocalsAudio,
            () => songMeta.VocalsAudio,
            newValue => songMeta.VocalsAudio = newValue);
        CreateSongPropertiesInputControl(ESongProperty.InstrumentalAudio,
            () => songMeta.InstrumentalAudio,
            newValue => songMeta.InstrumentalAudio = newValue);

        songMeta.AdditionalHeaderEntries.ForEach(entry =>
        {
            CreateSongPropertiesInputControl(ESongProperty.Other,
                () => songMeta.AdditionalHeaderEntries[entry.Key],
                newValue => songMeta.SetAdditionalHeaderEntry(entry.Key, newValue),
                "",
                Translation.Of(entry.Key));
        });
    }

    private void CreateSongPropertiesInputControl(
        ESongProperty songProperty,
        Func<string> valueGetter,
        Action<string> valueSetter,
        string unitName = "",
        Translation labelText = default)
    {
        labelText = !labelText.Value.IsNullOrEmpty() ? labelText : Translation.Get(songProperty);

        VisualElement visualElement = songPropertySideBarEntryUi.CloneTree().Children().First();
        songPropertiesSideBarContainer.Add(visualElement);

        TextField textField = visualElement.Q<TextField>(R.UxmlNames.propertyTextField);
        textField.DisableParseEscapeSequences();
        textField.SetTranslatedLabel(unitName.IsNullOrEmpty() ? labelText : Translation.Of($"{labelText} ({unitName})"));
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
                    songMetaChangedEventStream.OnNext(new SongPropertyChangedEvent(songProperty));
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
