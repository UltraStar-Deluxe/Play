using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UniInject;
using UnityEngine;

public class EditModeLyricsConverterTest
{
    private static int noteCount;
    
    // Test data with Russian terms, where a space can occur inside a single note.
    private static readonly List<string> lyricsToCreateNotes = new() {
        "Freu-de, |schö-ner |Göt-ter-fun-ken, |Toch-ter |aus |E-ly-si-um",
        "Wir |be-tre-ten |feuer-trun-ken, |Him-mli-sche, |dein |Hei-lig-tum.",
        "Dei-ne |Zau-ber |bin-den |wie-der, |was |die |Mo-de |streng |ge-teilt,",
        "al-le |Men-schen |wer-den |Brü-der, |wo |dein |sanf-ter |Flü-gel |weilt.",
        "К_тебе, |В_этом, |С_тобой"}; // Use underscore instead of space for easy splitting of notes.

    private static readonly List<string> expectedViewModeLyrics = new() {
        "Freude, schöner Götterfunken, Tochter aus Elysium",
        "Wir betreten feuertrunken, Himmlische, dein Heiligtum.",
        "Deine Zauber binden wieder, was die Mode streng geteilt,",
        "alle Menschen werden Brüder, wo dein sanfter Flügel weilt.",
        "К тебе, В этом, С тобой",
        "" // ViewMode lyrics generation always adds an empty line at the end.
    };

    private static readonly List<string> expectedDefaultEditModeLyrics = new() {
        "Freu;de, schö;ner Göt;ter;fun;ken, Toch;ter aus E;ly;si;um",
        "Wir be;tre;ten feuer;trun;ken, Him;mli;sche, dein Hei;lig;tum.",
        "Dei;ne Zau;ber bin;den wie;der, was die Mo;de streng ge;teilt,",
        "al;le Men;schen wer;den Brü;der, wo dein sanf;ter Flü;gel weilt.",
        // Space inside note is escaped
        "К\\ тебе, В\\ этом, С\\ тобой"};

    private static readonly List<string> expectedCustomEditModeLyrics = new() {
        "Freu`de,|schö`ner|Göt`ter`fun`ken,|Toch`ter|aus|E`ly`si`um",
        "Wir|be`tre`ten|feuer`trun`ken,|Him`mli`sche,|dein|Hei`lig`tum.",
        "Dei`ne|Zau`ber|bin`den|wie`der,|was|die|Mo`de|streng|ge`teilt,",
        "al`le|Men`schen|wer`den|Brü`der,|wo|dein|sanf`ter|Flü`gel|weilt.",
        // Space inside note is escaped
        "К\\ тебе,|В\\ этом,|С\\ тобой"};

    private static Voice CreateVoice()
    {
        Voice voice = new Voice(EVoiceId.P1, CreateSentences());
        
        // Check notes match expectations.
        List<Note> allNotes = SongMetaUtils.GetAllNotes(voice);
        Assert.AreEqual("Freu", allNotes[0].Text);
        Assert.AreEqual("de, ", allNotes[1].Text);
        Assert.AreEqual("schö", allNotes[2].Text);
        Assert.AreEqual("ner ", allNotes[3].Text);
        Assert.AreEqual("С тобой", allNotes.Last().Text);

        return voice;
    }

    private static List<Sentence> CreateSentences()
    {
        return new List<Sentence>(
            lyricsToCreateNotes.Select(line => CreateSentence(line))
        );
    }

    private static Sentence CreateSentence(string line)
    {
        return new Sentence(new Regex(@"-|\|").Split(line)
            // Replace underscore with space after splitting the notes.
            .Select(word => CreateNote(word.Replace("_", " ")))
            .ToList());
    }

    private static Note CreateNote(string word)
    {
        return new Note(ENoteType.Normal, noteCount++, 1, 0, word);
    }
    
    [Test]
    public void ShouldConvertToDefaultEditModeLyrics()
    {
        EditModeLyricsConverter converter = CreateEditModeLyricsConverter(new Settings());

        // When
        string actual = converter.GetEditModeText(CreateVoice());

        // Then
        string expected = expectedDefaultEditModeLyrics.JoinWith("\n");
        Debug.Log("expected: " + expected);
        Debug.Log("actual: " + actual);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void ShouldConvertToCustomEditModeLyrics()
    {
        // Given
        Settings customSettings = new()
        {
            SongEditorSettings =
            {
                WordSeparator = '|',
                SyllableSeparator = '`',
            }
        };
        EditModeLyricsConverter converter = CreateEditModeLyricsConverter(customSettings);

        // When
        string actual = converter.GetEditModeText(CreateVoice());

        // Then
        string expected = expectedCustomEditModeLyrics.JoinWith("\n");
        Debug.Log("expected: " + expected);
        Debug.Log("actual: " + actual);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void ShouldConvertToViewModeLyrics()
    {
        // Given
        EditModeLyricsConverter converter = CreateEditModeLyricsConverter(new Settings());

        // When
        string actual = converter.GetViewModeText(CreateVoice());

        // Then
        string expected = expectedViewModeLyrics.JoinWith("\n");
        Debug.Log("expected: " + expected);
        Debug.Log("actual: " + actual);
        Assert.AreEqual(expected, actual);
    }
    
    private static EditModeLyricsConverter CreateEditModeLyricsConverter(Settings settings)
    {
        EditModeLyricsConverter converter = new();
        UniInjectUtils.CreateInjector()
            .WithBindingForInstance(settings)
            .Inject(converter);
        return converter;
    }
}
