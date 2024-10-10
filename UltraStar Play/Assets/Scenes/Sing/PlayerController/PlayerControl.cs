using System;
using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using UniInject;
using UniInject.Extensions;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public PlayerNoteRecorder PlayerNoteRecorder { get; private set; }

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public PlayerMicPitchTracker PlayerMicPitchTracker { get; private set; }

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public PlayerPerformanceAssessmentControl PlayerPerformanceAssessmentControl { get; private set; }

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public PlayerScoreControl PlayerScoreControl { get; private set; }

    [Inject]
    public PlayerProfile PlayerProfile { get; private set; }

    [Inject(Key = nameof(playerProfileIndex))]
    private int playerProfileIndex;

    [Inject(Optional = true)]
    public MicProfile MicProfile { get; private set; }

    [Inject]
    public Voice Voice { get; private set; }

    [Inject(Key = nameof(playerUi))]
    private VisualTreeAsset playerUi;

    [Inject(Key = nameof(playerInfoUi))]
    private VisualTreeAsset playerInfoUi;

    [Inject(UxmlName = R.UxmlNames.playerInfoUiListBottomLeft)]
    private VisualElement playerInfoUiListBottomLeft;

    [Inject(UxmlName = R.UxmlNames.playerInfoUiListBottomRight)]
    private VisualElement playerInfoUiListBottomRight;

    [Inject(UxmlName = R.UxmlNames.playerInfoUiListTopLeft)]
    private VisualElement playerInfoUiListTopLeft;

    [Inject(UxmlName = R.UxmlNames.playerInfoUiListTopRight)]
    private VisualElement playerInfoUiListTopRight;

    [Inject]
    private SingSceneData sceneData;

    private readonly Subject<EnterSentenceEvent> enterSentenceEventStream = new();
    public IObservable<EnterSentenceEvent> EnterSentenceEventStream => enterSentenceEventStream;

    // The sorted sentences of the Voice
    public List<Sentence> SortedSentences { get; private set; } = new();

    public int MaxBeatInVoice => SortedSentences.LastOrDefault()?.ExtendedMaxBeat ?? 0;

    [Inject]
    private Injector injector;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private Settings settings;

    [Inject]
    private AchievementEventStream achievementEventStream;

    public PlayerUiControl PlayerUiControl { get; private set; } = new();

    // An injector with additional bindings, such as the PlayerProfile and the MicProfile.
    private Injector childrenInjector;

    private int displaySentenceIndex;

    private int perfectSentenceCount;

    private List<Note> sortedNotesInVoice;
    private List<Sentence> sortedSentencesInVoice;

    public void OnInjectionFinished()
    {
        childrenInjector = CreateChildrenInjectorWithAdditionalBindings();

        sortedNotesInVoice = SongMetaUtils.GetAllNotes(Voice);
        sortedNotesInVoice.Sort(Note.comparerByStartBeat);

        sortedSentencesInVoice = Voice.Sentences.ToList();
        sortedSentencesInVoice.Sort(Sentence.comparerByStartBeat);

        SortedSentences = Voice.Sentences.ToList();
        SortedSentences.Sort(Sentence.comparerByStartBeat);

        // Create UI
        VisualElement playerUiVisualElement = playerUi.CloneTree().Children().First();
        playerUiVisualElement.userData = this;
        VisualElement playerInfoUiVisualElement = playerUiVisualElement.Q(R.UxmlNames.playerInfoContainer);
        playerInfoUiVisualElement.userData = this;
        if (!settings.ShowPlayerInfoNextToNotes)
        {
            // Move player info UI to top / bottom
            AddPlayerInfoUiToTopOrBottom(playerInfoUiVisualElement);
        }

        // Inject all children.
        // The injector hierarchy is searched from the bottom up.
        // Thus, we can create an injection hierarchy with elements that are not necessarily in the same VisualElement hierarchy.
        foreach (INeedInjection childThatNeedsInjection in gameObject.GetComponentsInChildren<INeedInjection>(true))
        {
            if (childThatNeedsInjection is not PlayerControl)
            {
                childrenInjector.Inject(childThatNeedsInjection);
            }
        }

        // The UiControl must be injected last because it depends on the other controls
        Injector playerUiControlInjector = childrenInjector.CreateChildInjector()
            .WithRootVisualElement(playerInfoUiVisualElement)
            .CreateChildInjector()
            .WithRootVisualElement(playerUiVisualElement);
        playerUiControlInjector.Inject(PlayerUiControl);

        PlayerMicPitchTracker.MicProfile = MicProfile;

        SetDisplaySentenceIndex(0);

        InitAchievements();
    }

    private void InitAchievements()
    {
        if (CommonOnlineMultiplayerUtils.IsRemotePlayerProfile(PlayerProfile))
        {
            return;
        }

        PlayerPerformanceAssessmentControl.SentenceAssessedEventStream.Subscribe(evt =>
        {
            if (evt.IsPerfect)
            {
                perfectSentenceCount++;
                if (perfectSentenceCount > 10
                    && PlayerProfile.Difficulty is EDifficulty.Medium or EDifficulty.Hard)
                {
                    achievementEventStream.OnNext(new AchievementEvent(AchievementId.getMoreThan10PerfectRatingsInASong, PlayerProfile));
                }
            }
        });
    }

    private void AddPlayerInfoUiToTopOrBottom(VisualElement playerInfoUiVisualElement)
    {
        bool hasTopPlayerInfoUiRow = (sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count > 1
                                      && sceneData.SingScenePlayerData.PlayerProfileToVoiceIdMap.Values
                                          .Distinct()
                                          .Count() > 1)
                                     || sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count > 8;

        List<Voice> voices = songMeta.Voices
            .OrderBy(voice => voice.Id)
            .ToList();
        int voiceIndex = voices.IndexOf(Voice);
        if (hasTopPlayerInfoUiRow
            && voiceIndex <= 0)
        {
            // Prefer position near the top lyrics
            List<VisualElement> playerInfoUiLists = new()
            {
                playerInfoUiListTopLeft,
                playerInfoUiListTopRight,
                playerInfoUiListBottomLeft,
                playerInfoUiListBottomRight,
            };

            AddPlayerInfoUiToFreePlayerInfoUiList(playerInfoUiVisualElement, playerInfoUiLists);
        }
        else
        {
            // Prefer position near the bottom lyrics
            List<VisualElement> playerInfoUiLists = new()
            {
                playerInfoUiListBottomLeft,
                playerInfoUiListBottomRight,
                playerInfoUiListTopLeft,
                playerInfoUiListTopRight,
            };

            AddPlayerInfoUiToFreePlayerInfoUiList(playerInfoUiVisualElement, playerInfoUiLists);
        }
    }

    private void AddPlayerInfoUiToFreePlayerInfoUiList(VisualElement playerInfoUiVisualElement, List<VisualElement> playerInfoUiLists)
    {
        VisualElement playerInfoUiList = playerInfoUiLists.FirstOrDefault(it => it.childCount < 4);
        if (playerInfoUiList == null)
        {
            playerInfoUiList = playerInfoUiLists.LastOrDefault();
        }
        playerInfoUiList.Add(playerInfoUiVisualElement);
    }

    public void UpdateUi()
    {
        PlayerUiControl.Update();
    }

    private Injector CreateChildrenInjectorWithAdditionalBindings()
    {
        Injector newInjector = UniInjectUtils.CreateInjector(injector);
        newInjector.AddBindingForInstance(PlayerMicPitchTracker);
        newInjector.AddBindingForInstance(PlayerNoteRecorder);
        newInjector.AddBindingForInstance(PlayerPerformanceAssessmentControl);
        newInjector.AddBindingForInstance(PlayerScoreControl);
        newInjector.AddBindingForInstance(PlayerUiControl);
        newInjector.AddBindingForInstance(newInjector);
        newInjector.AddBindingForInstance(this);
        return newInjector;
    }

    public void SetCurrentBeat(double currentBeat)
    {
        // Change the current display sentence, when the current beat is over its last note.
        if (displaySentenceIndex < SortedSentences.Count
            && currentBeat >= GetDisplaySentence().LinebreakBeat)
        {
            Sentence nextDisplaySentence = GetUpcomingSentenceForBeat(currentBeat);
            int nextDisplaySentenceIndex = SortedSentences.IndexOf(nextDisplaySentence);
            if (nextDisplaySentenceIndex < 0)
            {
                // After last sentence
                SetDisplaySentenceIndex(SortedSentences.Count);
            }
            else
            {
                SetDisplaySentenceIndex(nextDisplaySentenceIndex);
            }
        }
    }

    private void SetDisplaySentenceIndex(int newValue)
    {
        displaySentenceIndex = newValue;

        Sentence displaySentence = GetSentence(displaySentenceIndex);

        // Update the UI
        enterSentenceEventStream.OnNext(new EnterSentenceEvent(displaySentence, displaySentenceIndex));
    }

    public IReadOnlyList<Note> GetSortedNotesInVoice()
    {
        return sortedNotesInVoice;
    }

    public IReadOnlyList<Sentence> GetSortedSentencesInVoice()
    {
        return sortedSentencesInVoice;
    }

    public Sentence GetSentence(int index)
    {
        Sentence sentence = (index >= 0 && index < SortedSentences.Count) ? SortedSentences[index] : null;
        return sentence;
    }

    public Note GetNextSingableNote(double currentBeat)
    {
        Note nextSingableNote = SortedSentences
            .SelectMany(sentence => sentence.Notes)
            // Freestyle notes are not displayed and not sung.
            // They do not contribute to the score.
            .Where(note => !note.IsFreestyle)
            .Where(note => currentBeat <= note.StartBeat)
            .OrderBy(note => note.StartBeat)
            .FirstOrDefault();
        return nextSingableNote;
    }

    private Sentence GetUpcomingSentenceForBeat(double currentBeat)
    {
        if (SortedSentences.IsNullOrEmpty())
        {
            return null;
        }

        Sentence result = SortedSentences
            .FirstOrDefault(sentence => currentBeat < sentence.LinebreakBeat);
        return result;
    }

    public Sentence GetDisplaySentence()
    {
        return GetSentence(displaySentenceIndex);
    }

    public Note GetLastNoteInSong()
    {
        if (SortedSentences.IsNullOrEmpty())
        {
            return null;
        }
        return SortedSentences
            .LastOrDefault()
            .Notes
            .OrderBy(note => note.EndBeat)
            .LastOrDefault();
    }

    public void SkipToBeat(int beat)
    {
        PlayerScoreControl.SkipToBeat(beat);
        PlayerMicPitchTracker.SkipToBeat(beat);
        Debug.Log($"Skipped forward to beat {beat} for player {PlayerProfile.Name}");
    }

    public class EnterSentenceEvent
    {
        public Sentence Sentence { get; private set; }
        public int SentenceIndex { get; private set; }

        public EnterSentenceEvent(Sentence sentence, int sentenceIndex)
        {
            Sentence = sentence;
            SentenceIndex = sentenceIndex;
        }
    }
}
