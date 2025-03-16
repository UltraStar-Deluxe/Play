using System;
using System.Collections.Generic;
using System.Linq;
using CircularBuffer;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ClientSideMicDataSender : AbstractMicPitchTracker, INeedInjection
{
    public static ClientSideMicDataSender Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<ClientSideMicDataSender>("ClientSideMicrophoneDataSender");
        }
    }

    [Inject]
    private Settings companionAppSettings;

    [Inject]
    private ClientSideCompanionClientManager clientSideCompanionClientManager;

    private readonly CircularBuffer<PositionInSongData> receivedPositionInSongTimes = new(3);
    private PositionInSongData bestPositionInSongData;
    private SongMeta songMeta;
    private int lastAnalyzedBeat;

    private bool HasPositionInSong => songMeta != null && bestPositionInSongData != null;

    private IDisposable receivedMessageStreamDisposable;

    private readonly Subject<MicProfile> micProfileChangedEventStream = new();
    public IObservable<MicProfile> MicProfileChangedEventStream => micProfileChangedEventStream;

    private readonly Subject<BeatPitchEventsDto> beatPitchEventsDtoEventStream = new();
    public IObservable<BeatPitchEventsDto> BeatPitchEventsDtoEventStream => beatPitchEventsDtoEventStream;

    private void Start()
    {
        ResetPositionInSong();

        clientSideCompanionClientManager.ConnectEventStream
            .Subscribe(UpdateConnectionStatus)
            .AddTo(gameObject);
        RecordingEventStream.Subscribe(evt => OnRecordingEvent(evt));
        IsRecording
            .Subscribe(HandleRecordingStatusChanged)
            .AddTo(gameObject);
    }

    protected override void Update()
    {
        base.Update();

        if (HasPositionInSong
            && bestPositionInSongData.UnixTimeInMillisWhenReceivedPositionInSong + 30000 < TimeUtils.GetUnixTimeMilliseconds())
        {
            // Did not receive new position in song for some time. Probably not in sing scene anymore.
            ResetPositionInSong();
        }
    }

    private void HandleRecordingStatusChanged(bool isRecording)
    {
        if (isRecording && HasPositionInSong)
        {
            // Analyze the following beats, not past beats.
            lastAnalyzedBeat = (int)SongMetaBpmUtils.MillisToBeats(songMeta, GetEstimatedPositionInSongInMillis());
            Log.Debug(() => $"HandleRecordingStatusChanged - lastAnalyzedBeat: {lastAnalyzedBeat}");
        }
    }

    private void OnRecordingEvent(RecordingEvent recordingEvent)
    {
        // Do pitch detection
        if (HasPositionInSong)
        {
            AnalyzeMicSamplesCorrespondingToBeatsInSong(recordingEvent);
        }
        else
        {
            AnalyzeNewestMicSamples(recordingEvent);
        }
    }

    private void AnalyzeMicSamplesCorrespondingToBeatsInSong(RecordingEvent recordingEvent)
    {
        if (micProfile == null
            || MicSampleRecorder == null)
        {
            return;
        }

        // Check if can analyze new beat
        double estimatedPositionInSongInMillis = GetEstimatedPositionInSongInMillis();
        double positionInSongConsideringMicDelay = estimatedPositionInSongInMillis - MicProfile.DelayInMillis;
        int currentBeatConsideringMicDelay = (int)SongMetaBpmUtils.MillisToBeats(songMeta, positionInSongConsideringMicDelay);
        if (currentBeatConsideringMicDelay <= lastAnalyzedBeat
            // Do not start analyzing beats too much before the first lyrics (typically at beat 0 when GAP is set correctly)
            || currentBeatConsideringMicDelay < -20)
        {
            return;
        }

        // Do not analyze more than 100 beats (might missed some beats while app was in background)
        int firstNextBeatToAnalyze = Math.Max(lastAnalyzedBeat + 1, currentBeatConsideringMicDelay - 100);

        List<BeatPitchEvent> beatPitchEvents = new();
        int loopCount = 0;
        int maxLoopCount = 100;
        for (int beat = firstNextBeatToAnalyze; beat <= currentBeatConsideringMicDelay; beat++)
        {
            PitchEvent pitchEvent = AnalyzeMicSamplesOfBeat(recordingEvent, beat, estimatedPositionInSongInMillis);
            int midiNote = pitchEvent?.MidiNote ?? -1;
            float frequency = pitchEvent?.Frequency ?? -1;
            // int midiNote = MidiUtils.MidiNoteConcertPitch;
            // float frequency = MidiUtils.MidiNoteConcertPitchFrequency;
            beatPitchEvents.Add(new BeatPitchEvent(midiNote, beat, frequency));

            loopCount++;
            if (loopCount > maxLoopCount)
            {
                // Emergency exit
                Debug.LogWarning($"Took emergency exit out of loop. Analyzed {maxLoopCount} beats and still not finished?");
            }
        }

        // Send all events in one message
        List<BeatPitchEventDto> beatPitchEventDtos = beatPitchEvents
            .Select(it => new BeatPitchEventDto(it.MidiNote, it.Beat, it.Frequency))
            .ToList();
        if (beatPitchEventDtos.Count > 3)
        {
            Debug.LogWarning($"Sending {beatPitchEventDtos.Count} beats to server: {beatPitchEventDtos.Select(it => it.Beat).JoinWith(", ")}");
        }

        BeatPitchEventsDto beatPitchEventsDto = new BeatPitchEventsDto(beatPitchEventDtos)
        {
            UnixTimeMilliseconds = TimeUtils.GetUnixTimeMilliseconds(),
        };

        beatPitchEventsDtoEventStream.OnNext(beatPitchEventsDto);
        SendMessageToServer(beatPitchEventsDto);

        lastAnalyzedBeat = currentBeatConsideringMicDelay;
    }

    private void AnalyzeNewestMicSamples(RecordingEvent recordingEvent)
    {
        if (micProfile == null
            || MicSampleRecorder == null)
        {
            return;
        }

        PitchEvent pitchEvent = AudioSamplesAnalyzer.ProcessAudioSamples(
            recordingEvent.MicSamples,
            recordingEvent.NewSamplesStartIndex,
            recordingEvent.NewSamplesEndIndex,
            MicProfile.AmplificationMultiplier,
            MicProfile.NoiseSuppression);

        int midiNote = pitchEvent?.MidiNote ?? -1;
        float frequency = pitchEvent?.Frequency ?? -1;
        BeatPitchEventDto beatPitchEventDto = new(midiNote, -1, frequency);
        BeatPitchEventsDto beatPitchEventsDto = new(beatPitchEventDto)
        {
            UnixTimeMilliseconds = TimeUtils.GetUnixTimeMilliseconds(),
        };

        beatPitchEventsDtoEventStream.OnNext(beatPitchEventsDto);
        SendMessageToServer(beatPitchEventsDto);
    }

    private void SendMessageToServer(JsonSerializable jsonSerializable)
    {
        Log.Verbose(() => $"SendMessageToServer - method: {companionAppSettings.MicDataDeliveryMethod}, message: " + jsonSerializable.ToJson());
        clientSideCompanionClientManager.SendMessageToServer(jsonSerializable, companionAppSettings.MicDataDeliveryMethod);
    }

    private double GetEstimatedPositionInSongInMillis()
    {
        if (!HasPositionInSong)
        {
            Debug.LogWarning("GetEstimatedPositionInSongInMillis called without position in song");
            return 0;
        }

        return bestPositionInSongData.EstimatedPositionInSongInMillis;
    }

    private PitchEvent AnalyzeMicSamplesOfBeat(RecordingEvent recordingEvent, int beat, double positionInSongInMillis)
    {
        if (micProfile == null
            || MicSampleRecorder == null)
        {
            return null;
        }

        PitchEvent pitchEvent = AnalyzeBeat(
            songMeta,
            beat,
            positionInSongInMillis,
            MicSampleRecorder.FinalSampleRate.Value,
            MicProfile.DelayInMillis,
            MicProfile.AmplificationMultiplier,
            MicProfile.NoiseSuppression,
            recordingEvent.MicSamples,
            AudioSamplesAnalyzer);
        return pitchEvent;
    }

    private void HandleMicProfileMessage(MicProfileMessageDto micProfileMessageDto)
    {
        Debug.Log($"Received new mic profile: {micProfileMessageDto.ToJson()}");

        MicProfile newMicProfile = new(companionAppSettings.MicProfile.Name);
        newMicProfile.Amplification = micProfileMessageDto.Amplification;
        newMicProfile.NoiseSuppression = micProfileMessageDto.NoiseSuppression;
        newMicProfile.SampleRate = micProfileMessageDto.SampleRate;
        newMicProfile.DelayInMillis = micProfileMessageDto.DelayInMillis;
        newMicProfile.Color = Colors.CreateColor(micProfileMessageDto.HexColor);

        MicProfile = newMicProfile;
        companionAppSettings.MicProfile = newMicProfile;
        micProfileChangedEventStream.OnNext(newMicProfile);
    }

    private void HandlePositionInSongMessage(PositionInSongDto positionInSongDto)
    {
        PositionInSongData positionInSongData = new(positionInSongDto.PositionInSongInMillis, TimeUtils.GetUnixTimeMilliseconds());
        receivedPositionInSongTimes.PushBack(positionInSongData);
        songMeta = new UltraStarSongMeta
        {
            BeatsPerMinute = positionInSongDto.BeatsPerMinute,
            GapInMillis = positionInSongDto.SongGap,
        };

        // If beats have been analyzed prematurely, then redo analysis.
        int currentBeat = (int)SongMetaBpmUtils.MillisToBeats(songMeta, GetEstimatedPositionInSongInMillis());
        if (lastAnalyzedBeat > currentBeat)
        {
            lastAnalyzedBeat = currentBeat;
        }

        // Use the "received position in the song" which has the least discrepancy
        // with respect to the "estimated position in song" of previously received times.
        // This makes the time more resilient against outliers (e.g. when a message was delivered with big delay).
        float GetTimeError(PositionInSongData time)
        {
            double resultError = 0;
            receivedPositionInSongTimes
                .Where(otherTime => otherTime.UnixTimeInMillisWhenReceivedPositionInSong != time.UnixTimeInMillisWhenReceivedPositionInSong)
                .ForEach(otherTime =>
                {
                    double offset = Math.Abs(time.EstimatedPositionInSongInMillis - otherTime.EstimatedPositionInSongInMillis);
                    resultError += offset;
                });
            return (float)resultError;
        }
        bestPositionInSongData = receivedPositionInSongTimes.FindMinElement(time => GetTimeError(time));

        Log.Debug(() => $"Received position in song: {positionInSongDto.ToJson()}, new best position in song {bestPositionInSongData.ToJson()}");
    }

    private void ResetPositionInSong()
    {
        Log.Debug(() => "Resetting position in song");
        songMeta = null;
        bestPositionInSongData = null;
        receivedPositionInSongTimes.Clear();
        lastAnalyzedBeat = -1;
    }

    private void UpdateConnectionStatus(ConnectEvent connectEvent)
    {
        if (receivedMessageStreamDisposable != null)
        {
            receivedMessageStreamDisposable.Dispose();
            receivedMessageStreamDisposable = null;
        }

        if (connectEvent.IsSuccess
            && clientSideCompanionClientManager.IsConnected)
        {
            receivedMessageStreamDisposable = clientSideCompanionClientManager.ReceivedMessageStream
                .Subscribe(dto =>
                {
                    if (dto is StopRecordingMessageDto)
                    {
                        Debug.Log("Stopping recording because of message from server");
                        StopRecording();
                        ResetPositionInSong();
                    }
                    else if (dto is StartRecordingMessageDto)
                    {
                        Debug.Log("Starting recording because of message from server");
                        StartRecording();
                    }
                    else if (dto is PositionInSongDto positionInSongDto)
                    {
                        HandlePositionInSongMessage(positionInSongDto);
                    }
                    else if (dto is MicProfileMessageDto micProfileMessageDto)
                    {
                        HandleMicProfileMessage(micProfileMessageDto);
                    }
                })
                .AddTo(gameObject);
        }
    }

    private class PositionInSongData : JsonSerializable
    {
        public double ReceivedPositionInSongInMillis { get; private set; }
        public long UnixTimeInMillisWhenReceivedPositionInSong { get; private set; }
        public double EstimatedPositionInSongInMillis => ReceivedPositionInSongInMillis + (TimeUtils.GetUnixTimeMilliseconds() - UnixTimeInMillisWhenReceivedPositionInSong);

        public PositionInSongData(double receivedPositionInSongInMillis, long unixTimeInMillisWhenReceivedPositionInSong)
        {
            ReceivedPositionInSongInMillis = receivedPositionInSongInMillis;
            UnixTimeInMillisWhenReceivedPositionInSong = unixTimeInMillisWhenReceivedPositionInSong;
        }
    }
}
