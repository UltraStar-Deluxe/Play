using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using static ConditionTestUtils;
using static SceneConditionTestUtils;
using static VisualElementTestUtils;

public class SongEditorLrcFormatImportTest : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.SongEditorScene.ToString();

    private static readonly string lrcExample = TrimStartOfEachLine(@"
        [00:00.88]Freude, schöner Götterfunken, Tochter aus Elysium
        [00:10.16]Wir betreten feuertrunken, Himmlische, dein Heiligtum.
        [00:19.58]Deine Zauber binden wieder, was die Mode streng geteilt,
        [00:29.19]alle Menschen werden Brüder, wo dein sanfter Flügel weilt.");

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private SongEditorLayerManager songEditorLayerManager;

    [UnityTest]
    public IEnumerator ShouldImportLrcFormat() => ShouldImportLrcFormatAsync();

    public async Awaitable ShouldImportLrcFormatAsync()
    {
        // Given
        await ExpectScene(EScene.SongEditorScene);

        // When
        await ClickButton(R.UxmlNames.openImportLrcDialogButton);
        await SetElementValue(R.UxmlNames.importLrcTextField, lrcExample);
        await ClickButton(R.UxmlNames.importLrcFormatButton);

        // Then
        await ExpectImportedNotes();
    }

    private async Task ExpectImportedNotes()
    {
        await WaitForCondition(() =>
            {
                List<Note> importedNotes = songEditorLayerManager.GetLayerNotes(songEditorLayerManager.GetEnumLayer(ESongEditorLayer.Import));
                return importedNotes.Count == 30
                       && SongMetaUtils.GetLyrics(importedNotes).StartsWith("Freude, schöner Götterfunken");
            }, new WaitForConditionConfig {description = "expect notes have been imported"});
    }

    private static string TrimStartOfEachLine(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        string[] lines = input.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].TrimStart();
        }
        return string.Join("\n", lines);
    }
}
