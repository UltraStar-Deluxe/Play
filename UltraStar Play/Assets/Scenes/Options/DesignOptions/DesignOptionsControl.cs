using System.Collections.Generic;
using ProTrans;
using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DesignOptionsControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    [Inject]
    private ThemeManager themeManager;

    [Inject(UxmlName = R.UxmlNames.themePicker)]
    private ItemPicker themePicker;

    [Inject(UxmlName = R.UxmlNames.imageAsCursorPicker)]
    private ItemPicker imageAsCursorPicker;

    [Inject(UxmlName = R.UxmlNames.sceneChangeAnimationPicker)]
    private ItemPicker sceneChangeAnimationPicker;

    [Inject(UxmlName = R.UxmlNames.sceneChangeDurationPicker)]
    private ItemPicker sceneChangeDurationPicker;
    
    [Inject(UxmlName = R.UxmlNames.animatedBackgroundItemPicker)]
    private ItemPicker animatedBackgroundItemPicker;
    
    [Inject(UxmlName = R.UxmlNames.backgroundLightItemPicker)]
    private ItemPicker backgroundLightItemPicker;
    
    [Inject(UxmlName = R.UxmlNames.vfxEnabledPicker)]
    private ItemPicker vfxEnabledPicker;

    [Inject(UxmlName = R.UxmlNames.showScrollBarInSongSelectPicker)]
    private ItemPicker showScrollBarInSongSelectPicker;
    
    [Inject]
    private UiManager uiManager;
    
    [Inject]
    private BackgroundLightManager backgroundLightManager;

    protected override void Start()
    {
        base.Start();
        
        new BoolPickerControl(imageAsCursorPicker)
            .Bind(() => settings.UseImageAsCursor,
                newValue => settings.UseImageAsCursor = newValue);

        new LabeledItemPickerControl<ESceneChangeAnimation>(sceneChangeAnimationPicker, EnumUtils.GetValuesAsList<ESceneChangeAnimation>())
            .Bind(() => settings.SceneChangeAnimation,
                newValue => settings.SceneChangeAnimation = newValue);

        new BoolPickerControl(vfxEnabledPicker)
            .Bind(() => settings.EnableVfx, 
                newValue => settings.EnableVfx = newValue);

        new BoolPickerControl(showScrollBarInSongSelectPicker)
            .Bind(() => settings.ShowScrollBarInSongSelect, 
                newValue => settings.ShowScrollBarInSongSelect = newValue);

        LabeledItemPickerControl<float> sceneChangeDurationPickerControl = new(sceneChangeDurationPicker, NumberUtils.CreateFloatList(0, 0.55f, 0.05f));
        sceneChangeDurationPickerControl.Bind(() => settings.SceneChangeDurationInSeconds,
                newValue => settings.SceneChangeDurationInSeconds = newValue);
        sceneChangeDurationPickerControl.GetLabelTextFunction = newValue => $"{newValue.ToStringInvariantCulture("0.00")} s";
        
        new BoolPickerControl(animatedBackgroundItemPicker)
            .Bind(() => settings.AnimatedBackground,
                newValue => settings.AnimatedBackground = newValue);
        
        new LabeledItemPickerControl<int>(backgroundLightItemPicker, NumberUtils.CreateIntList(0, backgroundLightManager.BackgroundLightInstancesCount))
            .Bind(() => settings.BackgroundLightIndex,
                newValue => settings.BackgroundLightIndex = newValue);
        
        // Load available themes:
        List<ThemeMeta> themeMetas = themeManager.GetThemeMetas();
        LabeledItemPickerControl<ThemeMeta> themePickerControl = new(themePicker, themeMetas);
        themePickerControl.GetLabelTextFunction = themeMeta => ThemeMetaUtils.GetDisplayName(themeMeta);
        themePickerControl.Bind(
            () => themeManager.GetCurrentTheme(),
            newValue => ChangeTheme(newValue));
    }

    private void ChangeTheme(ThemeMeta themeMeta)
    {
        if (themeManager.GetCurrentTheme() == themeMeta)
        {
            return;
        }
        ApplyThemeStyleUtils.ClearCache();
        themeManager.SetCurrentTheme(themeMeta);
    }

    public void UpdateTranslation()
    {
        themePicker.Label = TranslationManager.GetTranslation(R.Messages.options_design_theme);
        imageAsCursorPicker.Label = TranslationManager.GetTranslation(R.Messages.options_useImageAsCursor);
    }

    public override bool HasHelpDialog => true;
    public override MessageDialogControl CreateHelpDialogControl()
    {
        Dictionary<string, string> titleToContentMap = new()
        {
            { TranslationManager.GetTranslation(R.Messages.options_design_helpDialog_customThemes_title),
                TranslationManager.GetTranslation(R.Messages.options_design_helpDialog_customThemes,
                    "path", ApplicationUtils.ReplacePathsWithDisplayString(ThemeManager.GetAbsoluteUserDefinedThemesFolder())) },
        };
         MessageDialogControl helpDialogControl = uiManager.CreateHelpDialogControl(
            TranslationManager.GetTranslation(R.Messages.options_design_helpDialog_title),
            titleToContentMap);
        helpDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.viewMore),
            _ => Application.OpenURL(TranslationManager.GetTranslation(R.Messages.uri_howToAddCustomThemes)));
        helpDialogControl.AddButton("Themes Folder",
            _ => ApplicationUtils.OpenDirectory(ThemeManager.GetAbsoluteUserDefinedThemesFolder()));
        return helpDialogControl;
    }
}
