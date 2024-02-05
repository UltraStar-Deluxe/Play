using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Responsible;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Responsible.Responsibly;
using static ResponsibleSceneUtils;
using static ResponsibleVisualElementUtils;

public class CoreGameLoopTests : AbstractPlayModeTest
{
    private const float TestSongAudioLengthInSeconds = 4;

    // 3/4 of the notes should be hit.
    private static readonly int expectedScore = (int)(0.75 * PlayerScoreControl.maxScoreForNotes);

    protected override string TestSceneName => EScene.SongSelectScene.ToString();

    private PlayerProfile testPlayerProfile;
    private MicProfile testMicProfile;

    protected override List<string> GetRelativeTestSongFilePaths()
    {
        return new List<string>
        {
            "SingingTestSongs/ThreeQuartersA4OneQuarterC5-TestSong.txt",
        };
    }

    protected override void ConfigureTestSettings(TestSettings settings)
    {
        // Disable joker rule for simple note hit or miss definition.
        settings.JokerRuleEnabled = false;

        testPlayerProfile = new PlayerProfile("TestPlayer1", EDifficulty.Medium);
        settings.PlayerProfiles = new List<PlayerProfile>()
        {
            testPlayerProfile,
        };

        testMicProfile = new MicProfile("TestMic1");
        settings.MicProfiles = new List<MicProfile>()
        {
            testMicProfile,
        };

        // Song select should automatically assign the last used mic to the player.
        settings.PlayerProfileNameToLastUsedMicProfile.Add(testPlayerProfile.Name, new MicProfileReference(testMicProfile));

        // Simulate connected mic with A4 pitch frequency
        SimulatedMicrophoneAdapter.SetSimulatedDevices(new List<string>()
        {
            testMicProfile.Name,
        });
        SimulatedMicrophoneAdapter.SetSimulatedDevicePitchInHz(testPlayerProfile.Name, 440);
    }

    [UnityTest]
    public IEnumerator CoreGameLoopTest() => StartSinging()
        .ContinueWith(_ => WaitForSeconds(TestSongAudioLengthInSeconds + 2))
        .ContinueWith(_ => ExpectScene(EScene.SingingResultsScene))
        .ContinueWith(_ => ExpectSingingResultScore(expectedScore))
        .ContinueWith(_ => ExpectSingleHighscoreEntryInStatistics(expectedScore))
        .ContinueWith(_ => ClickContinue())
        .ContinueWith(_ => ExpectScene(EScene.SongSelectScene))
        .ToYieldInstruction(Executor);

    private ITestInstruction<object> ClickContinue()
        => GetElement<Button>(R.UxmlNames.continueButton)
            .ContinueWith(continueButton => ClickButton(continueButton));

    private ITestInstruction<object> StartSinging()
        => Do(
            $"start singing via enter key",
            () => InputFixture.PressAndRelease(Keyboard.enterKey))
        .ContinueWith(_ => ExpectScene(EScene.SingScene));

    private ITestInstruction<object> ExpectSingleHighscoreEntryInStatistics(int score)
        => WaitForCondition(
            $"expect single highscore entry with {score} points",
            () =>
            {
                List<HighScoreEntry> highScoreEntries = StatisticsManager.Instance.Statistics.LocalStatistics
                    .SelectMany(it => it.Value.HighScoreRecord.HighScoreEntries)
                    .ToList();
                return highScoreEntries.Count() == 1
                       && highScoreEntries.FirstOrDefault().Score == score;
            }).ExpectWithinSeconds(1);

    private ITestInstruction<object> ExpectSingingResultScore(int score)
        => GetElement<Label>(R.UxmlNames.totalScoreLabel)
            .ContinueWith(totalScoreLabel => WaitForCondition(
                $"score label shows {score}",
                () => totalScoreLabel.text == score.ToString())
                .ExpectWithinSeconds(5));
}
