using System;
using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

/**
 * Analyzes each beat of a player in the sing scene.
 * Thereby, it applies some additional rounding an joker rules.
 */
public class PlayerMicPitchTracker : AbstractMicPitchTracker, INeedInjection, IInjectionFinishedListener
{
    private const int SendPositionInSongIntervalInMillis = 2000;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private PlayerControl playerControl;

    [Inject]
    private PlayerProfile playerProfile;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    [Inject]
    private SingSceneMedleyControl medleyControl;

    [Inject]
    private Settings mainGameSettings;

    [Inject]
    private OnlineMultiplayerManager onlineMultiplayerManager;

    [Inject]
    private Injector injector;

    // The rounding distance of the PlayerProfile
    private float roundingDistance;

    private int recordingSentenceIndex;

    private int beatToAnalyze;
    public int BeatToAnalyze { get; private set; }

    public Sentence RecordingSentence { get; private set; }
    private List<Note> currentAndUpcomingNotesInRecordingSentence;

    private IAudioSamplesAnalyzer audioSamplesAnalyzer;

    private bool hasJoker;

    // Only for debugging: see how many jokers have been used in the inspector
    [ReadOnly]
    public int usedJokerCount;

    private readonly Subject<BeatAnalyzedEvent> beatAnalyzedEventStream = new();
    public IObservable<BeatAnalyzedEvent> BeatAnalyzedEventStream => beatAnalyzedEventStream
        .ObserveOnMainThread();

    private readonly Subject<NoteAnalyzedEvent> noteAnalyzedEventStream = new();
    public IObservable<NoteAnalyzedEvent> NoteAnalyzedEventStream => noteAnalyzedEventStream
        .ObserveOnMainThread();

    private readonly Subject<SentenceAnalyzedEvent> sentenceAnalyzedEventStream = new();
    public IObservable<SentenceAnalyzedEvent> SentenceAnalyzedEventStream => sentenceAnalyzedEventStream
        .ObserveOnMainThread();

    private int lastAnalyzedBeatFromConnectedClient;

    private long lastUnixTimeMillisecondsWhenSentPositionInSongToClient = TimeUtils.GetUnixTimeMilliseconds();

    private readonly Queue<BeatPitchEventAndTime> beatPitchEventsFromConnectedClientQueue = new();

    private readonly List<IDisposable> disposables = new();

    protected override MicSampleRecorder MicSampleRecorder
    {
        get
        {
            if (playerProfile is LobbyMemberPlayerProfile
                && playerProfile != onlineMultiplayerManager.OwnLobbyMemberPlayerProfile)
            {
                // Cannot record mic samples for other lobby members
                return null;
            }

            return base.MicSampleRecorder;
        }
    }

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();

        // Find first sentence to analyze
        SetRecordingSentence(recordingSentenceIndex);

        roundingDistance = playerProfile.Difficulty.GetRoundingDistanceInMidiNotes();
        BeatAnalyzedEventStream.Subscribe(evt => OnBeatAnalyzed(evt));

        InitOnlineMultiplayer();
    }

    private void InitOnlineMultiplayer()
    {
        if (onlineMultiplayerManager.IsOnlineGame)
        {
            disposables.Add(onlineMultiplayerManager.MessagingControl.RegisterNamedMessageHandler(
                nameof(BeatAnalyzedEvent),
                message => OnBeatAnalyzedEventMessage(message)));
        }
    }

    private void OnBeatAnalyzedEventMessage(NamedMessage message)
    {
        if (playerProfile is not LobbyMemberPlayerProfile lobbyMemberPlayerProfile)
        {
            Log.Verbose(() => $"Ignoring BeatAnalyzedEventNetcodeRequestDto from Netcode client {message.SenderNetcodeClientId} because this PlayerMicPitchTracker handles a local player");
            return;
        }

        if (message.SenderNetcodeClientId != lobbyMemberPlayerProfile.UnityNetcodeClientId)
        {
            Log.Verbose(() => $"Ignoring BeatAnalyzedEventNetcodeRequestDto from Netcode client {message.SenderNetcodeClientId} because this PlayerMicPitchTracker handles client {lobbyMemberPlayerProfile.UnityNetcodeClientId} with name '{playerProfile.Name}'");
            return;
        }

        ReadBeatAnalyzedEventFastBufferReader(
            message.MessagePayload,
            out int midiNote,
            out float frequency,
            out int beat,
            out int recordedMidiNote,
            out int roundedRecordedMidiNote);

        FireBeatAnalyzedEventFromRemote(
            message.SenderNetcodeClientId,
            midiNote > 0 ? midiNote : 0,
            frequency > 0 ? frequency : 0,
            beat,
            recordedMidiNote,
            roundedRecordedMidiNote);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        disposables.ForEach(it => it.Dispose());
    }

    public void InitPitchDetection()
    {
        if (micProfile == null
            || MicSampleRecorder == null
            || (playerProfile is LobbyMemberPlayerProfile lobbyMemberPlayerProfile
                && playerProfile != onlineMultiplayerManager.OwnLobbyMemberPlayerProfile))
        {
            return;
        }

        if (micProfile.IsInputFromConnectedClient)
        {
            InitPitchDetectionFromConnectedClient();
            serverSideConnectRequestManager.ClientConnectionChangedEventStream
                .Where(evt => evt.IsConnected)
                .Subscribe(_ => OnClientConnectionChanged())
                .AddTo(gameObject);
        }
        else
        {
            InitPitchDetectionFromLocalMicrophone();
        }
    }

    private void OnClientConnectionChanged()
    {
        InitPitchDetectionFromConnectedClient();
    }

    private void InitPitchDetectionFromLocalMicrophone()
    {
        if (micProfile == null
            || MicSampleRecorder == null)
        {
            return;
        }

        MicSampleRecorder.StartRecording();

        // The AudioSampleAnalyzer uses the MicSampleRecorder's sampleRateHz. Thus, it must be initialized after the MicSampleRecorder.
        audioSamplesAnalyzer = CreateAudioSamplesAnalyzer(mainGameSettings.PitchDetectionAlgorithm, MicSampleRecorder.FinalSampleRate.Value);
    }

    private void InitPitchDetectionFromConnectedClient()
    {
        IConnectedClientHandler connectedClientHandler = GetConnectedClientHandler();
        if (connectedClientHandler == null)
        {
            Debug.LogWarning($"Did not find connected client handler for player {playerProfile.Name}. Not recording player notes.");
            return;
        }

        connectedClientHandler.ReceivedMessageStream
            .Subscribe(dto =>
            {
                if (dto is BeatPitchEventDto beatPitchEventDto)
                {
                    EnqueuePitchEventFromConnectedClient(beatPitchEventDto);
                }
                else if (dto is BeatPitchEventsDto beatPitchEventsDto)
                {
                    EnqueuePitchEventsFromConnectedClient(beatPitchEventsDto);
                }
            })
            .AddTo(gameObject);

        SendMicProfileToConnectedClient();
        SendStartRecordingMessageToConnectedClient();
        SendPositionInSongToClientRapidly();
    }

    private void EnqueuePitchEventsFromConnectedClient(BeatPitchEventsDto beatPitchEventsDto)
    {
        beatPitchEventsDto.BeatPitchEvents.ForEach(beatPitchEventDto =>
            EnqueuePitchEventFromConnectedClient(beatPitchEventDto));
    }

    private void EnqueuePitchEventFromConnectedClient(BeatPitchEventDto beatPitchEventDto)
    {
        beatPitchEventsFromConnectedClientQueue.Enqueue(new BeatPitchEventAndTime()
        {
            beatPitchEvent = new BeatPitchEvent(beatPitchEventDto.MidiNote, beatPitchEventDto.Beat, beatPitchEventDto.Frequency),
            unixTimeInMillis = TimeUtils.GetUnixTimeMilliseconds(),
        });
    }

    private IConnectedClientHandler GetConnectedClientHandler()
    {
        if (micProfile == null
            || !micProfile.IsInputFromConnectedClient)
        {
            return null;
        }

        serverSideConnectRequestManager.TryGetConnectedClientHandler(micProfile.ConnectedClientId, out IConnectedClientHandler connectedClientHandler);
        return connectedClientHandler;
    }

    protected override void Update()
    {
        base.Update();

        if (micProfile == null
            || MicSampleRecorder == null)
        {
            return;
        }

        if (micProfile.IsInputFromConnectedClient)
        {
            UpdatePitchDetectionFromConnectedClient();
        }
        else if (MicSampleRecorder.IsRecording.Value)
        {
            UpdatePitchDetectionFromLocalMicrophone();
        }
    }

    private void UpdatePitchDetectionFromLocalMicrophone()
    {
        // No sentence to analyze left (all done).
        if (RecordingSentence == null)
        {
            return;
        }

        // Analyze the next beat with fully recorded mic samples
        double nextBeatToAnalyzeEndPositionInMs = SongMetaBpmUtils.BeatsToMillis(songMeta, BeatToAnalyze + 1);
        if (nextBeatToAnalyzeEndPositionInMs >= songAudioPlayer.PositionInSongInMillis - micProfile.DelayInMillis)
        {
            return;
        }

        // The beat has passed and should have recorded samples in the mic buffer. Analyze the samples now.
        PitchEvent pitchEvent = GetPitchEventOfBeat(BeatToAnalyze);
        Note currentOrUpcomingNote = currentAndUpcomingNotesInRecordingSentence.IsNullOrEmpty()
            ? null
            : currentAndUpcomingNotesInRecordingSentence[0];
        Note noteAtBeat = (currentOrUpcomingNote.StartBeat <= BeatToAnalyze && BeatToAnalyze < currentOrUpcomingNote.EndBeat)
            ? currentOrUpcomingNote
            : null;

        FirePitchEvent(pitchEvent, BeatToAnalyze, noteAtBeat, RecordingSentence);
    }

    private void UpdatePitchDetectionFromConnectedClient()
    {
        IConnectedClientHandler connectedClientHandler = GetConnectedClientHandler();
        if (connectedClientHandler == null)
        {
            // Disconnected
            return;
        }

        // Read messages from client since last time the reader thread was active.
        // connectedClientHandler.ReadMessagesFromClient();

        if (lastUnixTimeMillisecondsWhenSentPositionInSongToClient + SendPositionInSongIntervalInMillis < TimeUtils.GetUnixTimeMilliseconds())
        {
            // Synchronize position in song with connected client.
            SendPositionInSongToClient();
        }

        // Handle received messages after buffer time.
        if (!beatPitchEventsFromConnectedClientQueue.IsNullOrEmpty())
        {
            long connectedClientMessageBufferTime = (long)Mathf.Max(mainGameSettings.ConnectedClientMessageBufferTimeInMillis, connectedClientHandler.JitterInMillis * 1.5f);
            int beatBufferTime = Mathf.Max(1, (int)SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, connectedClientMessageBufferTime * 1.5f));
            DequeuePitchEventsFromConnectedClient(connectedClientMessageBufferTime, beatBufferTime);
        }
    }

    private void DequeuePitchEventsFromConnectedClient(long messageBufferTimeInMillis, int eventBufferTimeInBeats)
    {
        int positionInSongInMillisConsideringMicDelay = (int)(songAudioPlayer.PositionInSongInMillis - micProfile.DelayInMillis);
        int currentBeatConsideringMicDelay = (int)SongMetaBpmUtils.MillisToBeats(songMeta, positionInSongInMillisConsideringMicDelay);
        long unixTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        int maxIterations = 100;
        for (int i = 0; i < maxIterations && !beatPitchEventsFromConnectedClientQueue.IsNullOrEmpty(); i++)
        {
            BeatPitchEventAndTime beatPitchEventAndTime = beatPitchEventsFromConnectedClientQueue.Peek();
            BeatPitchEvent beatPitchEvent = beatPitchEventAndTime.beatPitchEvent;
            // Handle the event when this was for an old message
            long messageAgeInMillis = Math.Abs(unixTimeInMillis - beatPitchEventAndTime.unixTimeInMillis);
            bool handleBecauseOfMessageBufferTime = messageAgeInMillis > messageBufferTimeInMillis;
            // if (handleBecauseOfMessageBufferTime)
            // {
            //     Log.Verbose(() => $"Handling old message with age {messageAgeInMillis} ms" + JsonConverter.ToJson(beatPitchEvent));
            // }

            int eventAgeInBeats = Math.Abs(currentBeatConsideringMicDelay - beatPitchEventAndTime.beatPitchEvent.Beat);
            bool handleBecauseOfEventBufferTime = eventAgeInBeats > eventBufferTimeInBeats;
            // if (!handleBecauseOfMessageBufferTime
            //     && handleBecauseOfEventBufferTime)
            // {
            //     Log.Verbose(() => $"Handling old event with age {eventAgeInBeats} beats: " + JsonConverter.ToJson(beatPitchEvent));
            // }

            if (handleBecauseOfMessageBufferTime
                || handleBecauseOfEventBufferTime)
            {
                // Remove from queue and handle
                beatPitchEventsFromConnectedClientQueue.Dequeue();
                HandlePitchEventFromConnectedClient(beatPitchEvent);
            }
            else
            {
                break;
            }
        }

        // Log.Verbose(() => "DequeuePitchEventsFromConnectedClient: Remaining events: " + beatPitchEventsFromConnectedClientQueue.Count);
    }

    private int ApplyJokerRule(PitchEvent pitchEvent, int roundedMidiNote, Note noteAtBeat)
    {
        if (noteAtBeat == null)
        {
            return roundedMidiNote;
        }

        // Earn a joker when singing correctly (without using a joker).
        // A failed beat can be undone via joker-rule.
        if (pitchEvent != null && roundedMidiNote == noteAtBeat.MidiNote)
        {
            hasJoker = true;
        }
        // The joker is only for continued singing.
        if (pitchEvent == null)
        {
            hasJoker = false;
        }

        // If the player fails a beat in continued singing, but the previous beats were sung correctly,
        // then this failed beat is ignored.
        if (roundedMidiNote != noteAtBeat.MidiNote
            && hasJoker)
        {
            hasJoker = false;
            usedJokerCount++;
            return noteAtBeat.MidiNote;
        }
        return roundedMidiNote;
    }

    private void HandlePitchEventFromConnectedClient(BeatPitchEvent pitchEvent)
    {
        if (pitchEvent.Beat < 0
            || pitchEvent.Beat < lastAnalyzedBeatFromConnectedClient)
        {
            // Looks like the companion app does not know the current position in the song. Send it this info again.
            // Log.Verbose($"Received invalid beat from connected client: beat {pitchEvent.Beat}");
            if (lastUnixTimeMillisecondsWhenSentPositionInSongToClient + (SendPositionInSongIntervalInMillis / 10) < TimeUtils.GetUnixTimeMilliseconds())
            {
                SendPositionInSongToClient();
            }
            return;
        }

        int currentBeat = (int)SongMetaBpmUtils.MillisToBeats(songMeta, songAudioPlayer.PositionInSongInMillis);
        if (pitchEvent.Beat > currentBeat)
        {
            Log.Verbose(() => $"Received future beat from connected client (received: {pitchEvent.Beat}, current: {currentBeat}).");
            return;
        }

        lastAnalyzedBeatFromConnectedClient = pitchEvent.Beat;
        FirePitchEventFromConnectedClient(pitchEvent);
    }

    public void SendPositionInSongToClientRapidly()
    {
        if (micProfile == null
            || !micProfile.IsInputFromConnectedClient)
        {
            return;
        }

        PlayerNoteRecorder playerNoteRecorder = GetComponent<PlayerNoteRecorder>();
        if (playerNoteRecorder != null
            && !playerNoteRecorder.isActiveAndEnabled)
        {
            // No need to synchronize with client.
            return;
        }

        // The position in the song changed dramatically.
        // But the client implements methods to ignore single messages with big position differences (resilient behavior).
        // Thus, send the new position in song more aggressively.
        List<float> delaysInSeconds = new(){ 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
        delaysInSeconds.ForEach(delayInSeconds =>
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(delayInSeconds, () => SendPositionInSongToClient())));
    }

    private void SendPositionInSongToClient()
    {
        lastUnixTimeMillisecondsWhenSentPositionInSongToClient = TimeUtils.GetUnixTimeMilliseconds();

        if (micProfile == null
            || !micProfile.IsInputFromConnectedClient)
        {
            return;
        }

        IConnectedClientHandler connectedClientHandler = GetConnectedClientHandler();
        if (connectedClientHandler == null)
        {
            // Disconnected
            return;
        }

        PositionInSongDto positionInSongDto = new PositionInSongDto
        {
            // TODO: Should exchange BeatsPerMinute instead of UltraStar txt file BPM
            SongBpm = songMeta.BeatsPerMinute / 4,
            SongGap = songMeta.GapInMillis,
            PositionInSongInMillis = songAudioPlayer.PositionInSongInMillisExact,
        };
        Log.Verbose(() => $"Send position in song to client {micProfile.ConnectedClientId}: {positionInSongDto.ToJson()}");
        connectedClientHandler.SendMessageToClient(positionInSongDto);
    }

    private void FirePitchEventFromConnectedClient(BeatPitchEvent pitchEvent)
    {
        if (pitchEvent.Beat < 0)
        {
            return;
        }

        Sentence sentenceAtBeat = SongMetaUtils.GetSentenceAtBeat(playerControl.Voice, pitchEvent.Beat, true, false);
        Note noteAtBeat = SongMetaUtils.GetNoteAtBeat(sentenceAtBeat, pitchEvent.Beat, true, false);
        int midiNote = pitchEvent.MidiNote;
        float frequency = pitchEvent.Frequency;
        if (midiNote < 0)
        {
            FirePitchEvent(null, pitchEvent.Beat, noteAtBeat, sentenceAtBeat);
        }
        else
        {
            FirePitchEvent(new PitchEvent(midiNote, frequency), pitchEvent.Beat, noteAtBeat, sentenceAtBeat);
        }
    }

    public void FirePitchEvent(PitchEvent pitchEvent, int beat, Note noteAtBeat, Sentence sentenceAtBeat)
    {
        if (beat < BeatToAnalyze)
        {
            // Ignore this event, the beat was already analyzed.
            return;
        }

        int recordedMidiNote = pitchEvent != null
            ? pitchEvent.MidiNote
            : -1;
        int roundedRecordedMidiNote = pitchEvent != null
            ? GetRoundedMidiNoteForRecordedMidiNote(noteAtBeat, pitchEvent.MidiNote, pitchEvent.Frequency)
            : -1;

        int roundedMidiNoteAfterJoker = ApplyJokerRule(pitchEvent, roundedRecordedMidiNote, noteAtBeat);

        BeatAnalyzedEvent beatAnalyzedEvent = new BeatAnalyzedEvent(
            pitchEvent,
            beat,
            noteAtBeat,
            sentenceAtBeat,
            recordedMidiNote,
            roundedMidiNoteAfterJoker);
        beatAnalyzedEventStream.OnNext(beatAnalyzedEvent);

        if (onlineMultiplayerManager.IsOnlineGame
            && playerProfile == onlineMultiplayerManager.OwnLobbyMemberPlayerProfile)
        {
            onlineMultiplayerManager.MessagingControl.SendNamedMessageToClients(
                nameof(BeatAnalyzedEvent),
                CreateBeatAnalyzedEventFastBufferWriter(beatAnalyzedEvent),
                onlineMultiplayerManager.OtherLobbyMembersUnityNetcodeClientIds,
                NetworkDelivery.Unreliable);
        }
    }

    private FastBufferWriter CreateBeatAnalyzedEventFastBufferWriter(BeatAnalyzedEvent beatAnalyzedEvent)
    {
        // The BeatAnalyzedEvent is fired often (multiple times per second) such that a fast (de)serialization is mandatory.
        // This is why a compact representation is created here instead of JSON.
        int size = FastBufferWriter.GetWriteSize<int>() // midiNote
                       + FastBufferWriter.GetWriteSize<float>() // frequency
                       + FastBufferWriter.GetWriteSize<int>() // beat
                       + FastBufferWriter.GetWriteSize<int>() // recordedMidiNote
                       + FastBufferWriter.GetWriteSize<int>(); // roundedRecordedMidiNote
        FastBufferWriter fastBufferWriter = new(size, Allocator.Temp);
        fastBufferWriter.WriteValueSafe(beatAnalyzedEvent.PitchEvent?.MidiNote ?? -1);
        fastBufferWriter.WriteValueSafe(beatAnalyzedEvent.PitchEvent?.Frequency ?? -1);
        fastBufferWriter.WriteValueSafe(beatAnalyzedEvent.Beat);
        fastBufferWriter.WriteValueSafe(beatAnalyzedEvent.RecordedMidiNote);
        fastBufferWriter.WriteValueSafe(beatAnalyzedEvent.RoundedRecordedMidiNote);
        return fastBufferWriter;
    }

    private void ReadBeatAnalyzedEventFastBufferReader(
        FastBufferReader fastBufferReader,
        out int midiNote,
        out float frequency,
        out int beat,
        out int recordedMidiNote,
        out int roundedRecordedMidiNote)
    {
        // The BeatAnalyzedEvent is fired often (multiple times per second) such that a fast (de)serialization is mandatory.
        // This is why a compact representation is read here instead of JSON.
        fastBufferReader.ReadValueSafe(out midiNote);
        fastBufferReader.ReadValueSafe(out frequency);
        fastBufferReader.ReadValueSafe(out beat);
        fastBufferReader.ReadValueSafe(out recordedMidiNote);
        fastBufferReader.ReadValueSafe(out roundedRecordedMidiNote);
    }

    private void FireBeatAnalyzedEventFromRemote(ulong senderNetcodeClientId, int midiNote, float frequency, int beat, int recordedMidiNote, int roundedRecordedMidiNote)
    {
        Log.Verbose(() => $"Fire BeatAnalyzedEvent from Netcode client {senderNetcodeClientId} (beat: {beat}, midiNote: {midiNote})");

        Note noteAtBeat = SongMetaUtils.GetNoteAtBeat(playerControl.GetSortedNotesInVoice(), beat);
        Sentence sentenceAtBeat = SongMetaUtils.GetSentenceAtBeat(playerControl.GetSortedSentencesInVoice(), beat);

        PitchEvent pitchEvent = midiNote > 0
            ? new PitchEvent(midiNote, frequency)
            : null;

        BeatAnalyzedEvent beatAnalyzedEvent = new(
            pitchEvent,
            beat,
            noteAtBeat,
            sentenceAtBeat,
            recordedMidiNote,
            roundedRecordedMidiNote);
        beatAnalyzedEventStream.OnNext(beatAnalyzedEvent);
    }

    private void OnBeatAnalyzed(BeatAnalyzedEvent beatAnalyzedEvent)
    {
        if (beatAnalyzedEvent == null
            || RecordingSentence == null)
        {
            return;
        }

        if (BeatToAnalyze <= beatAnalyzedEvent.Beat)
        {
            BeatToAnalyze = beatAnalyzedEvent.Beat + 1;
        }
        if (BeatToAnalyze > RecordingSentence.MaxBeat)
        {
            // All beats of the sentence analyzed. Go to next sentence.
            GoToNextRecordingSentence();
            return;
        }

        // If there is no note at that beat, then use the StartBeat of the following note for next analysis.
        // Remove notes that have been completely analyzed.
        Note passedNote = null;
        if (!currentAndUpcomingNotesInRecordingSentence.IsNullOrEmpty()
            && currentAndUpcomingNotesInRecordingSentence[0].EndBeat <= BeatToAnalyze)
        {
            passedNote = currentAndUpcomingNotesInRecordingSentence[0];
            currentAndUpcomingNotesInRecordingSentence.RemoveAt(0);
        }
        if (passedNote != null)
        {
            noteAnalyzedEventStream.OnNext(new NoteAnalyzedEvent(passedNote));
        }

        // Check if there is still a current note that is analyzed.
        if (!currentAndUpcomingNotesInRecordingSentence.IsNullOrEmpty())
        {
            Note currentOrUpcomingNote = currentAndUpcomingNotesInRecordingSentence[0];
            if (currentOrUpcomingNote.StartBeat > BeatToAnalyze
                && !mainGameSettings.AnalyzeBeatsWithoutTargetNote)
            {
                // Next beat to analyze is at the next note
                BeatToAnalyze = currentOrUpcomingNote.StartBeat;
            }
        }
        else if (mainGameSettings.AnalyzeBeatsWithoutTargetNote
                 && BeatToAnalyze < RecordingSentence.MaxBeat)
        {
            BeatToAnalyze++;
        }
        else
        {
            // All notes of the sentence analyzed. Go to next sentence.
            GoToNextRecordingSentence();
        }
    }

    private void GoToNextRecordingSentence()
    {
        // Fire event about finished sentence
        Sentence nextRecordingSentence = playerControl.GetSentence(recordingSentenceIndex + 1);
        sentenceAnalyzedEventStream.OnNext(new SentenceAnalyzedEvent(RecordingSentence, nextRecordingSentence == null));
        // Select next sentence
        recordingSentenceIndex++;
        SetRecordingSentence(recordingSentenceIndex);
    }

    private int GetMicSampleBufferIndexForBeat(int beat)
    {
        if (micProfile == null
            || MicSampleRecorder == null)
        {
            return 0;
        }

        double beatInMs = SongMetaBpmUtils.BeatsToMillis(songMeta, beat);
        double beatPassedBeforeMs = songAudioPlayer.PositionInSongInMillis - beatInMs;
        int beatPassedBeforeSamplesInMicBuffer = Convert.ToInt32(((beatPassedBeforeMs - micProfile.DelayInMillis) / 1000) * MicSampleRecorder.FinalSampleRate.Value);
        // The newest sample has the highest index in the MicSampleBuffer
        int sampleBufferIndex = MicSampleRecorder.MicSamples.Length - beatPassedBeforeSamplesInMicBuffer;
        sampleBufferIndex = NumberUtils.Limit(sampleBufferIndex, 0, MicSampleRecorder.MicSamples.Length - 1);
        return sampleBufferIndex;
    }

    private void SetRecordingSentence(int sentenceIndex)
    {
        if (sentenceIndex == 0
            && mainGameSettings.ShowPitchIndicator)
        {
            // Start with very first beat, possibly before the lyrics start to update the pitch indicator.
            BeatToAnalyze = (int)SongMetaBpmUtils.MillisToBeats(songMeta, 0);
        }

        RecordingSentence = playerControl.GetSentence(sentenceIndex);
        if (RecordingSentence == null)
        {
            // After last sentence or no sentences at all.
            // Wait until the mic has finished recording the last note.
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(1f,
                () =>
                {
                    currentAndUpcomingNotesInRecordingSentence = new List<Note>();
                    BeatToAnalyze = 0;
                }));
            return;
        }
        currentAndUpcomingNotesInRecordingSentence = SongMetaUtils.GetSortedNotes(RecordingSentence);

        if (mainGameSettings.ShowPitchIndicator)
        {
            // Analyze all beats to update pitch indicator.
            BeatToAnalyze++;
        }
        else
        {
            // Don't analyze until the next sentence is reached
            BeatToAnalyze = RecordingSentence.MinBeat;
        }
    }

    void OnDisable()
    {
        if (micProfile != null
            && MicSampleRecorder != null)
        {
            MicSampleRecorder.StopRecording();
        }
    }

    private int GetRoundedMidiNoteForRecordedMidiNote(Note targetNote, int recordedMidiNote, float recordedFrequency)
    {
        if (targetNote == null)
        {
            return recordedMidiNote;
        }

        if (targetNote.Type is ENoteType.Rap or ENoteType.RapGolden)
        {
            // Rap notes accept any noise as correct note.
            return targetNote.MidiNote;
        }
        else if (recordedMidiNote is < MidiUtils.SingableNoteMin or > MidiUtils.SingableNoteMax)
        {
            // The pitch detection can fail, which is the case when the detected pitch is outside of the singable note range.
            // In this case, just assume that the player was singing correctly and round to the target note.
            return targetNote.MidiNote;
        }
        else
        {
            // Round recorded note if it is close to the target note.
            return GetRoundedMidiNote(recordedMidiNote, recordedFrequency, targetNote.MidiNote, roundingDistance);
        }
    }

    private int GetRoundedMidiNote(int recordedMidiNote, float recordedFrequency, int targetMidiNote, float roundingDistance)
    {
        float exactMidiNote = recordedMidiNote;

        // Check if rounding a fraction of a midi note is possible and needed.
        if (recordedFrequency > 0)
        {
            // It is possible.
            float roundingDistanceDecimal = roundingDistance - Mathf.Floor(roundingDistance);
            if (roundingDistanceDecimal > 0)
            {
                // It is needed.
                exactMidiNote = MidiUtils.CalculateMidiNote(recordedFrequency);
            }
        }

        float distance = MidiUtils.GetRelativePitchDistance(exactMidiNote, targetMidiNote);
        return distance <= roundingDistance
            ? targetMidiNote
            : recordedMidiNote;
    }

    public void SkipToBeat(double currentBeat)
    {
        if (currentBeat < beatToAnalyze)
        {
            // Cannot jump back in song
            return;
        }

        // Find sentence to analyze next.
        RecordingSentence = playerControl.SortedSentences
            .FirstOrDefault(sentence => currentBeat <= sentence.MaxBeat);
        if (RecordingSentence != null)
        {
            recordingSentenceIndex = playerControl.SortedSentences.IndexOf(RecordingSentence);
            // Find note to analyze next
            currentAndUpcomingNotesInRecordingSentence = RecordingSentence.Notes
                .Where(note => currentBeat <= note.EndBeat)
                .OrderBy(note => note.StartBeat)
                .ToList();
            if (currentAndUpcomingNotesInRecordingSentence.Count > 0)
            {
                if (currentAndUpcomingNotesInRecordingSentence[0].StartBeat < currentBeat)
                {
                    // currentBeat is inside note
                    BeatToAnalyze = (int)currentBeat;
                }
                else
                {
                    // The note is upcoming, analyze its first beat next.
                    BeatToAnalyze = currentAndUpcomingNotesInRecordingSentence[0].StartBeat;
                }
            }
            else
            {
                BeatToAnalyze = RecordingSentence.MaxBeat;
            }
        }

        if (micProfile != null
            && micProfile.IsInputFromConnectedClient)
        {
            // Position changed heavily. Send the new position more aggressively to connected clients.
            SendPositionInSongToClientRapidly();
        }
    }

    private PitchEvent GetPitchEventOfSamples(int startSampleBufferIndex, int endSampleBufferIndex)
    {
        if (micProfile == null
            || MicSampleRecorder == null)
        {
            return null;
        }

        if (startSampleBufferIndex > endSampleBufferIndex)
        {
            ObjectUtils.Swap(ref startSampleBufferIndex, ref endSampleBufferIndex);
        }
        startSampleBufferIndex = NumberUtils.Limit(startSampleBufferIndex, 0, MicSampleRecorder.MicSamples.Length - 1);
        endSampleBufferIndex = NumberUtils.Limit(endSampleBufferIndex, 0, MicSampleRecorder.MicSamples.Length - 1);
        PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(MicSampleRecorder.MicSamples, startSampleBufferIndex, endSampleBufferIndex, micProfile.AmplificationMultiplier, micProfile.NoiseSuppression);
        return pitchEvent;
    }

    private PitchEvent GetPitchEventOfBeat(int beat)
    {
        int startSampleBufferIndex = GetMicSampleBufferIndexForBeat(beat);
        int endSampleBufferIndex = GetMicSampleBufferIndexForBeat(beat + 1);
        PitchEvent pitchEvent = GetPitchEventOfSamples(startSampleBufferIndex, endSampleBufferIndex);
        return pitchEvent;
    }

    private void SendMicProfileToConnectedClient()
    {
        GetConnectedClientHandler()?.SendMessageToClient(new MicProfileMessageDto(micProfile));
    }

    public void SendStopRecordingMessageToConnectedClient()
    {
        GetConnectedClientHandler()?.SendMessageToClient(new StopRecordingMessageDto());
    }

    public void SendStartRecordingMessageToConnectedClient()
    {
        GetConnectedClientHandler()?.SendMessageToClient(new StartRecordingMessageDto());
        SendPositionInSongToClientRapidly();
    }

    public override void StartRecording()
    {
        if (micProfile == null)
        {
            return;
        }

        if (micProfile.IsInputFromConnectedClient)
        {
            SendStartRecordingMessageToConnectedClient();
        }
        else
        {
            base.StartRecording();
        }
    }

    public override void StopRecording()
    {
        if (micProfile == null)
        {
            return;
        }

        if (micProfile.IsInputFromConnectedClient)
        {
            SendStopRecordingMessageToConnectedClient();
        }
        else
        {
            base.StopRecording();
        }
    }

    private class BeatPitchEventAndTime
    {
        public BeatPitchEvent beatPitchEvent;
        public long unixTimeInMillis;
    }
}
