using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UniInject;
using UnityEngine;
using UniRx;
using CircularBuffer;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ClientSideMicDataSender : MonoBehaviour, INeedInjection
{
    public static ClientSideMicDataSender Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<ClientSideMicDataSender>("ClientSideMicrophoneDataSender");
        }
    }

    [Inject]
    private MicSampleRecorder micSampleRecorder;

    [Inject]
    private Settings settings;

    [Inject]
    private ClientSideConnectRequestManager clientSideConnectRequestManager;

    private IAudioSamplesAnalyzer audioSamplesAnalyzer;

    private readonly CircularBuffer<PositionInSongData> receivedPositionInSongTimes = new(3);
    private PositionInSongData bestPositionInSongData;
    private SongMeta songMeta;
    private int lastAnalyzedBeat;

    private bool HasPositionInSong => songMeta != null && bestPositionInSongData != null;

    private IDisposable receivedMessageStreamDisposable;

    private void Start()
    {
        ResetPositionInSong();

        UpdateAudioSamplesAnalyzer();
        micSampleRecorder.FinalSampleRate
            .Subscribe(_ => UpdateAudioSamplesAnalyzer())
            .AddTo(gameObject);

        clientSideConnectRequestManager.ConnectEventStream
            .Subscribe(UpdateConnectionStatus)
            .AddTo(gameObject);
        micSampleRecorder.RecordingEventStream
            .Subscribe(HandleNewMicSamples)
            .AddTo(gameObject);
        micSampleRecorder.IsRecording
            .Subscribe(HandleRecordingStatusChanged)
            .AddTo(gameObject);
    }

    private void Update()
    {
        if (HasPositionInSong
            && bestPositionInSongData.UnixTimeInMillisWhenReceivedPositionInSong + 30000 < TimeUtils.GetUnixTimeMilliseconds())
        {
            // Did not receive new position in song for some time. Probably not in sing scene anymore.
            ResetPositionInSong();
        }
    }

    private void UpdateAudioSamplesAnalyzer()
    {
        audioSamplesAnalyzer = AbstractMicPitchTracker.CreateAudioSamplesAnalyzer(EPitchDetectionAlgorithm.Dywa, micSampleRecorder.FinalSampleRate.Value);
    }

    private void HandleRecordingStatusChanged(bool isRecording)
    {
        if (isRecording && HasPositionInSong)
        {
            // Analyze the following beats, not past beats.
            lastAnalyzedBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, GetEstimatedPositionInSongInMillis());
            Debug.Log($"HandleRecordingStatusChanged - lastAnalyzedBeat: {lastAnalyzedBeat}");
        }
    }

    private void HandleNewMicSamples(RecordingEvent recordingEvent)
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
        // Check if can analyze new beat
        double estimatedPositionInSongInMillis = GetEstimatedPositionInSongInMillis();
        double positionInSongConsideringMicDelay = estimatedPositionInSongInMillis - settings.MicProfile.DelayInMillis;
        int currentBeatConsideringMicDelay = (int)BpmUtils.MillisecondInSongToBeat(songMeta, positionInSongConsideringMicDelay);
        if (currentBeatConsideringMicDelay <= lastAnalyzedBeat
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
            int midiNote = pitchEvent != null
                ? pitchEvent.MidiNote
                : -1;
            beatPitchEvents.Add(new BeatPitchEvent(midiNote, beat));

            loopCount++;
            if (loopCount > maxLoopCount)
            {
                // Emergency exit
                Debug.LogWarning($"Took emergency exit out of loop. Analyzed {maxLoopCount} beats and still not finished?");
            }
        }

        // Send all events int one message
        List<BeatPitchEventDto> beatPitchEventDtos = beatPitchEvents
            .Select(it => new BeatPitchEventDto(it.MidiNote, it.Beat))
            .ToList();
        if (beatPitchEventDtos.Count > 3)
        {
            Debug.LogWarning($"Sending {beatPitchEventDtos.Count} beats to server: {beatPitchEventDtos.Select(it => it.Beat).ToCsv(", ")}");
        }
        SendMessageToServer(new BeatPitchEventsDto(beatPitchEventDtos));

        lastAnalyzedBeat = currentBeatConsideringMicDelay;
    }

    private void AnalyzeNewestMicSamples(RecordingEvent recordingEvent)
    {
        PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(
            recordingEvent.MicSamples,
            recordingEvent.NewSamplesStartIndex,
            recordingEvent.NewSamplesEndIndex,
            GetMicProfileWithFinalSampleRate());

        int midiNote = pitchEvent != null
            ? pitchEvent.MidiNote
            : -1;
        BeatPitchEventDto beatPitchEventDto = new(midiNote, -1);
        SendMessageToServer(new BeatPitchEventsDto(beatPitchEventDto));
    }

    private void SendMessageToServer(JsonSerializable jsonSerializable)
    {
        if (clientSideConnectRequestManager.TryGetConnectedServerHandler(out IConnectedServerHandler connectedServerHandler))
        {
            connectedServerHandler.SendMessageToServer(jsonSerializable);
        }
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
        PitchEvent pitchEvent = AbstractMicPitchTracker.AnalyzeBeat(
            songMeta,
            beat,
            positionInSongInMillis,
            GetMicProfileWithFinalSampleRate(),
            recordingEvent.MicSamples,
            audioSamplesAnalyzer);
        return pitchEvent;
    }

    private MicProfile GetMicProfileWithFinalSampleRate()
    {
        // The MicProfile in the settings may use a SampleRate of 0 for "best available".
        // The pitch detection algorithm needs the proper value.
        MicProfile micProfile = new(settings.MicProfile);
        micProfile.SampleRate = micSampleRecorder.FinalSampleRate.Value;
        return micProfile;
    }

    private void HandleMicProfileMessage(MicProfileMessageDto micProfileMessageDto)
    {
        Debug.Log($"Received new mic profile: {micProfileMessageDto.ToJson()}");

        MicProfile micProfile = new(settings.MicProfile.Name);
        micProfile.Amplification = micProfileMessageDto.Amplification;
        micProfile.NoiseSuppression = micProfileMessageDto.NoiseSuppression;
        micProfile.SampleRate = micProfileMessageDto.SampleRate;
        micProfile.DelayInMillis = micProfileMessageDto.DelayInMillis;
        micProfile.Color = Colors.CreateColor(micProfileMessageDto.HexColor);

        settings.MicProfile = micProfile;
    }

    private void HandlePositionInSongMessage(PositionInSongDto positionInSongDto)
    {
        PositionInSongData positionInSongData = new(positionInSongDto.PositionInSongInMillis, TimeUtils.GetUnixTimeMilliseconds());
        receivedPositionInSongTimes.PushBack(positionInSongData);
        songMeta = new SongMeta
        {
            Bpm = positionInSongDto.SongBpm,
            Gap = positionInSongDto.SongGap,
        };

        // If beats have been analyzed prematurely, then redo analysis.
        int currentBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, GetEstimatedPositionInSongInMillis());
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

        Debug.Log($"Received position in song: {positionInSongDto.ToJson()}, new best position in song {bestPositionInSongData.ToJson()}");
    }

    private void ResetPositionInSong()
    {
        Debug.Log("Resetting position in song");
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
            && clientSideConnectRequestManager.TryGetConnectedServerHandler(out IConnectedServerHandler connectedServerHandler))
        {
            receivedMessageStreamDisposable = connectedServerHandler.ReceivedMessageStream
                .ObserveOnMainThread()
                .Subscribe(dto =>
                {
                    if (dto is StopRecordingMessageDto)
                    {
                        Debug.Log("Stopping recording because of message from server");
                        micSampleRecorder.StopRecording();
                        ResetPositionInSong();
                    }
                    else if (dto is StartRecordingMessageDto)
                    {
                        Debug.Log("Starting recording because of message from server");
                        micSampleRecorder.StartRecording();
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
