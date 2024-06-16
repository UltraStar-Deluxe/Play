using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimeInputActions;
using Truncon.Collections;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class AboutSceneControl : MonoBehaviour, INeedInjection
{
    private static readonly InsertionOrderedDictionary<string, string> aboutTextFilesInStreamingAssets = new()
    {
        { "Melody Mania", "InfoAndLegalTexts/Melody-Mania.txt" },
        { "Licenses", "InfoAndLegalTexts/License-Overview.txt" },
        { "MIT License", "InfoAndLegalTexts/MIT-License.txt" },
        { "APL 2.0", "InfoAndLegalTexts/APL-2.0.txt" },
        { "MPL 1.1", "InfoAndLegalTexts/MPL-1.1.txt" },
        { "MPL 2.0", "InfoAndLegalTexts/MPL-2.0.txt" },
        { "BGM", "InfoAndLegalTexts/BGM.txt" },
        { "Soundfont", "InfoAndLegalTexts/GeneralUser-GS-Soundfont-License.txt" },
        { "Pixabay", "InfoAndLegalTexts/Pixabay-Content-License.txt" },
        { "CEF", "InfoAndLegalTexts/Chromium-Embedded-Framework.txt" },

        // Libraries used by "Ffmpeg for Unity"
        { "BSD 2-Clause", "InfoAndLegalTexts/BSD-2-Clause.txt" },
        { "BSD 3-Clause", "InfoAndLegalTexts/BSD-3-Clause.txt" },
        { "LGPGv2.1", "InfoAndLegalTexts/lgpl-2.1.txt" },
        { "LGPGv3", "InfoAndLegalTexts/lgpl-3.0.txt" },
        { "openh264", "InfoAndLegalTexts/openh264-Binary-License.txt" },
        { "png", "InfoAndLegalTexts/libpng-License.txt" },
        { "zlib", "InfoAndLegalTexts/zlib-License.txt" },
    };

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.aboutTextScrollView)]
    private ScrollView aboutTextScrollView;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject(UxmlName = R.UxmlNames.aboutTextsScrollView)]
    private ScrollView aboutTextsScrollView;

    private int selectedTextIndex;

    private readonly List<ToggleButton> toggleButtons = new();
    private ToggleButton lastActiveToggleButton;

    private void Start()
    {
        CreateAboutTextButtons();

        KeyValuePair<string,string> initialAboutTextEntry = aboutTextFilesInStreamingAssets.FirstOrDefault();
        ShowAboutText(initialAboutTextEntry.Key, LoadAboutText(initialAboutTextEntry.Value));

        backButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.MainScene));
        backButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.MainScene));
    }

    private void CreateAboutTextButtons()
    {
        aboutTextsScrollView.Clear();
        toggleButtons.Clear();
        aboutTextFilesInStreamingAssets.ForEach(CreateAboutTextButton);

        ToggleButton firstToggleButton = toggleButtons.FirstOrDefault();
        firstToggleButton.SetActive(true);
        lastActiveToggleButton = firstToggleButton;
    }

    private void CreateAboutTextButton(KeyValuePair<string, string> aboutTextEntry)
    {
        ToggleButton button = new();
        button.AddToClassList("mb-2");
        button.SetTranslatedText(Translation.Of(aboutTextEntry.Key));
        button.RegisterCallbackButtonTriggered(_ =>
        {
            ShowAboutText(aboutTextEntry.Key, LoadAboutText(aboutTextEntry.Value));

            if (lastActiveToggleButton != null)
            {
                lastActiveToggleButton.SetActive(false);
            }
            lastActiveToggleButton = button;

            button.SetActive(true);
        });
        aboutTextsScrollView.Add(button);

        toggleButtons.Add(button);
    }

    private string LoadAboutText(string filePathInStreamingAssets)
    {
        string fullPath = ApplicationUtils.GetStreamingAssetsPath(filePathInStreamingAssets);
        if (!FileUtils.Exists(fullPath))
        {
            Debug.LogError($"About text not found: {fullPath}");
            return "";
        }
        return File.ReadAllText(fullPath);
    }

    private void ShowAboutText(string title, string text)
    {
        aboutTextScrollView.Clear();

        // A Unity label has a maximum length. So the text needs to be split into multiple labels.
        // Otherwise there is a warning message: "Generated text will be truncated because it exceeds 49152 vertices"
        // Split text into parts of 10000 characters.
        int maxCharactersPerLabel = 10000;
        int numberOfLabels = 1 + (text.Length / 10000);
        if (numberOfLabels > 1)
        {
            Debug.Log($"Splitting about text '{title}' into {numberOfLabels} labels.");
        }

        string[] textParts = new string[numberOfLabels];
        for (int i = 0; i < numberOfLabels; i++)
        {
            int startIndex = i * maxCharactersPerLabel;
            int length = Math.Min(maxCharactersPerLabel, text.Length - startIndex);
            textParts[i] = text.Substring(startIndex, length);

            TextField textField = new TextField();
            textField.DisableParseEscapeSequences();
            textField.isReadOnly = true;
            textField.pickingMode = PickingMode.Ignore;
            textField.AddToClassList("multiline");
            textField.AddToClassList("noBackground");
            textField.AddToClassList("aboutTextField");
            textField.value = textParts[i];
            aboutTextScrollView.Add(textField);
        }
    }
}
