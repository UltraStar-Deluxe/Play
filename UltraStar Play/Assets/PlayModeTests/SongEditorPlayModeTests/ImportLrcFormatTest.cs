using System;
using System.Collections;
using System.Collections.Generic;
using Responsible;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using static Responsible.Responsibly;
using static ResponsibleVisualElementUtils;
using static ResponsibleSceneUtils;
using static ResponsibleLogAssertUtils;

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
    private List<Note> ImportedNotes => songEditorLayerManager.GetLayerNotes(songEditorLayerManager.GetEnumLayer(ESongEditorLayer.Import));

    [UnityTest]
    public IEnumerator ShouldImportLrcFormat() => IgnoreFailingMessages()
        .ContinueWith(ExpectScene(EScene.SongEditorScene))
        .ContinueWith(_ => ClickButton(R.UxmlNames.openImportLrcDialogButton))
        .ContinueWith(_ => SetElementValue(R.UxmlNames.importLrcTextField, lrcExample))
        .ContinueWith(_ => ClickButton(R.UxmlNames.importLrcFormatDialogButton))
        .ContinueWith(_ => WaitForCondition("expect notes have been imported",
            () => ImportedNotes.Count == 30
                  && SongMetaUtils.GetLyrics(ImportedNotes).StartsWith("Freude, schöner Götterfunken"))
            .ExpectWithinSeconds(10))
        .ToYieldInstruction(Executor);

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
