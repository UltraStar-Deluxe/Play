using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static SceneConditionTestUtils;
using static VisualElementTestUtils;
using static ConditionUtils;

public class CoreGameLoopTest : AbstractPlayModeTest
{
    private const float TestSongAudioLengthInMillis = 4000;

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
    public IEnumerator CoreGameLoopWorksWithoutErrors() => CoreGameLoopWorksWithoutErrorsAsync();
    private async Awaitable CoreGameLoopWorksWithoutErrorsAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();
        await StartSingingAsync();
        await ExpectSceneAsync(EScene.SingScene);
        await ExpectSceneAsync(EScene.SingingResultsScene, new WaitForConditionConfig { timeoutInMillis = TestSongAudioLengthInMillis + 2000 });
        await ExpectSingingResultScoreAsync(expectedScore);
        await ExpectSingleHighscoreEntryInStatisticsAsync(expectedScore);
        await ClickContinueAsync();
        await ExpectSceneAsync(EScene.SongSelectScene);
    }

    private async Awaitable ClickContinueAsync()
    {
        Button continueButton = await GetElementAsync<Button>(R.UxmlNames.continueButton);
        await ClickButtonAsync(continueButton);
    }

    private async Awaitable StartSingingAsync()
    {
        InputFixture.PressAndRelease(Keyboard.enterKey);
        await Awaitable.WaitForSecondsAsync(0.1f);
    }

    private async Awaitable ExpectSingleHighscoreEntryInStatisticsAsync(int score)
    {
        await WaitForConditionAsync(() =>
        {
            List<HighScoreEntry> highScoreEntries = StatisticsManager.Instance.Statistics.LocalStatistics
                .SelectMany(it => it.Value.HighScoreRecord.HighScoreEntries)
                .ToList();
            return highScoreEntries.Count() == 1
                   && highScoreEntries.FirstOrDefault().Score == score;
        }, new WaitForConditionConfig { description = $"expect single highscore entry with {score} points" });
    }

    private async Awaitable ExpectSingingResultScoreAsync(int score)
    {
        Label totalScoreLabel = await GetElementAsync<Label>(R.UxmlNames.totalScoreLabel);
        await WaitForConditionAsync(
                () => totalScoreLabel.text == score.ToString(),
                new WaitForConditionConfig { description = $"score label shows {score}" });
    }
}
