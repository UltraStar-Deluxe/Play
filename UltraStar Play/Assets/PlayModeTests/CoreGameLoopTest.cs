using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Responsible;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Responsible.Responsibly;
using static ResponsibleSceneUtils;
using static ResponsibleLogAssertUtils;
using static ResponsibleVisualElementUtils;

public class CoreGameLoopTest : AbstractPlayModeTest
{
    private const float TestSongAudioLengthInSeconds = 4;

    // 3/4 of the notes should be hit.
    private static readonly int expectedScore = (int)(0.75 * PlayerScoreControl.maxScoreForNotes);

    protected override string TestSceneName => EScene.SongSelectScene.ToString();

    private PlayerProfile testPlayerProfile;
    private MicProfile testMicProfile;

    protected override void ConfigureTestSettings(TestSettings settings)
    {
        base.ConfigureTestSettings(settings);

        testPlayerProfile = settings.PlayerProfiles.FirstOrDefault();
        testMicProfile = settings.MicProfiles.FirstOrDefault();

        // Disable joker rule for simple note hit or miss definition.
        settings.JokerRuleEnabled = false;

        // Simulate connected mic with A4 pitch frequency
        SimulatedMicrophoneAdapter.SetSimulatedDevices(new List<string>() { testMicProfile.Name });
        SimulatedMicrophoneAdapter.SetSimulatedDevicePitchInHz(testPlayerProfile.Name, 440);
    }

    [UnityTest]
    public IEnumerator CoreGameLoopWorksWithoutErrors() => IgnoreFailingMessages()
        .ContinueWith(StartSinging())
        .ContinueWith(ExpectScene(EScene.SingScene))
        .ContinueWith(ExpectScene(EScene.SingingResultsScene, TestSongAudioLengthInSeconds + 2))
        .ContinueWith(ExpectSingingResultScore(expectedScore))
        .ContinueWith(ExpectSingleHighscoreEntryInStatistics(expectedScore))
        .ContinueWith(ClickContinue())
        .ContinueWith(ExpectScene(EScene.SongSelectScene))
        .ToYieldInstruction(Executor);

    private ITestInstruction<object> ClickContinue()
        => GetElement<Button>(R.UxmlNames.continueButton)
            .ContinueWith(continueButton => ClickButton(continueButton));

    private ITestInstruction<object> StartSinging()
        => Do(
            $"start singing via enter key",
            () => InputFixture.PressAndRelease(Keyboard.enterKey));

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
            }).ExpectWithinSeconds(10);

    private ITestInstruction<object> ExpectSingingResultScore(int score)
        => GetElement<Label>(R.UxmlNames.totalScoreLabel)
            .ContinueWith(totalScoreLabel => WaitForCondition(
                $"score label shows {score}",
                () => totalScoreLabel.text == score.ToString())
                .ExpectWithinSeconds(5));
}
