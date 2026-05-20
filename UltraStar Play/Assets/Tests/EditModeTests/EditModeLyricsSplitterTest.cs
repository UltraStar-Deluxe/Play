using System.Collections.Generic;
using NUnit.Framework;
using UniInject;
using UnityEngine;

public class EditModeLyricsSplitterTest
{
    [Test]
    public void ShouldSplitNote()
    {
        // Given
        Settings customSettings = new()
        {
            SongEditorSettings =
            {
                WordSeparator = '|',
            }
        };
        EditModeLyricsSplitter splitter = CreateEditModeLyricsSplitter(customSettings);

        // When
        Assert.IsTrue(splitter.TryApplyEditModeText(new Note(ENoteType.Normal, 0, 8, 0, "abcdefgi"), "abcdef|gi", out List<Note> notesAfterSplit));

        // Then
        Assert.AreEqual(2, notesAfterSplit.Count);
        Assert.AreEqual("abcdef|", notesAfterSplit[0].Text);
        Assert.AreEqual(6, notesAfterSplit[0].Length);
        
        // Note length should be proportional to position of separator
        Assert.AreEqual("gi", notesAfterSplit[1].Text);
        Assert.AreEqual(2, notesAfterSplit[1].Length);
    }

    [Test]
    public void ShouldPreserveNote()
    {
        // Given
        Settings customSettings = new()
        {
            SongEditorSettings =
            {
                WordSeparator = '|',
            }
        };
        EditModeLyricsSplitter splitter = CreateEditModeLyricsSplitter(customSettings);

        // When: Russian lyrics with space inside word.
        Assert.IsTrue(splitter.TryApplyEditModeText(new Note(ENoteType.Normal, 0, 10, 0, "К тебе"), "К тебе", out List<Note> notesAfterSplit));

        // Then
        Assert.AreEqual(1, notesAfterSplit.Count);
        Assert.AreEqual("К тебе", notesAfterSplit[0].Text);
    }

    [Test]
    public void ShouldPreserveNoteWithEscapedSpace()
    {
        // Given
        Settings customSettings = new();
        EditModeLyricsSplitter splitter = CreateEditModeLyricsSplitter(customSettings);

        // When: Russian lyrics with space inside word.
        Assert.IsTrue(splitter.TryApplyEditModeText(new Note(ENoteType.Normal, 0, 10, 0, "К\\ тебе"), "К\\ тебе", out List<Note> notesAfterSplit));

        // Then
        Assert.AreEqual(1, notesAfterSplit.Count);
        Assert.AreEqual("К\\ тебе", notesAfterSplit[0].Text);
    }
    
    private static EditModeLyricsSplitter CreateEditModeLyricsSplitter(Settings settings)
    {
        EditModeLyricsSplitter obj = new();
        UniInjectUtils.CreateInjector()
            .WithBindingForInstance(settings)
            .Inject(obj);
        return obj;
    }
}
