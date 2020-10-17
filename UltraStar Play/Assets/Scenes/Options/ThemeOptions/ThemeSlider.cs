using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ThemeSlider : TextItemSlider<Theme>, INeedInjection
{
    [Inject]
    private ThemeManager themeManager;

    [Inject]
    private Settings settings;

    protected override void Start()
    {
        base.Start();
        // The ThemeManager needs some time to load the available themes
        if (ThemeManager.HasFinishedLoadingThemes)
        {
            DoSetup();
        }
        else
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(5,
                () => ThemeManager.HasFinishedLoadingThemes,
                () => DoSetup()));
        }
    }

    private void DoSetup()
    {
        Items = ThemeManager.GetThemes();
        if (Items.Count == 0)
        {
            Debug.LogError("No themes have been loaded!");
            return;
        }

        Theme selectedTheme = Items.Where(theme => theme.Name == settings.GraphicSettings.themeName).FirstOrDefault();
        if (selectedTheme == null)
        {
            Selection.Value = Items[0];
        }
        else
        {
            Selection.Value = selectedTheme;
        }

        Selection.Subscribe(newValue =>
        {
            if (newValue == null)
            {
                return;
            }
            settings.GraphicSettings.themeName = newValue.Name;
            ThemeManager.CurrentTheme = newValue;
            ThemeManager.Instance.UpdateThemeResources();
        });
    }

    protected override string GetDisplayString(Theme value)
    {
        if (value == null)
        {
            return "";
        }

        return value.Name;
    }
}
