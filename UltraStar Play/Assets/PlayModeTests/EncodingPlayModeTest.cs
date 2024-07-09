using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UniInject;
using UnityEngine.TestTools;

public class EncodingPlayModeTest
{
    private static readonly List<TestCase> testCases = new()
    {
        new("Windows1252.txt", "Tränenüberströmt nach Fußmassage"),
        new("ISO-8859-1.txt", "Tränenüberströmt nach Fußmassage"),
        new("koi8-r.txt", "Я люблю тебя"),
    };

    public abstract class AbstractEncodingGuessingPlayModeTest : AbstractPlayModeTest
    {
        protected override string TestSceneName => EScene.SongSelectScene.ToString();

        protected override List<string> GetRelativeTestSongFilePaths()
        {
            return new List<string>()
            {
                "./Windows1252.txt",
                "./ISO-8859-1.txt",
                "./koi8-r.txt",
            };
        }

        [Inject]
        protected SongMetaManager songMetaManager;

        protected abstract bool ShouldBeEqual { get; }

        [UnityTest]
        public IEnumerator SongsShouldHaveExpectedTitle()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return null;

            foreach (TestCase testCase in testCases)
            {
                SongMeta matchingSongMeta = songMetaManager.GetSongMetas()
                    .FirstOrDefault(songMeta => songMeta.FileInfo.Name == testCase.fileName);
                if (ShouldBeEqual)
                {
                    Assert.AreEqual(testCase.title, matchingSongMeta.Title);
                }
                else
                {
                    Assert.AreNotEqual(testCase.title, matchingSongMeta.Title);
                }
            }
        }
    }

    public class EnabledEncodingGuessingPlayModeTest : AbstractEncodingGuessingPlayModeTest
    {
        protected override bool ShouldBeEqual => true;
    }

    public class DisabledEncodingGuessingPlayModeTest : AbstractEncodingGuessingPlayModeTest
    {
        protected override bool ShouldBeEqual => false;

        protected override void ConfigureTestSettings(TestSettings settings)
        {
            base.ConfigureTestSettings(settings);
            settings.UseUniversalCharsetDetector = false;
        }
    }

    private class TestCase
    {
        public string fileName;
        public string title;

        public TestCase(string fileName, string title)
        {
            this.fileName = fileName;
            this.title = title;
        }
    }
}
