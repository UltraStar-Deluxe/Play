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
    private ClientSideMicSampleRecorder clientSideMicSampleRecorder;

    [Inject]
    private Settings settings;

    [Inject]
    private ClientSideConnectRequestManager clientSideConnectRequestManager;
    
    private TcpClient tcpClient;
    private NetworkStream tcpClientStream;
    private StreamReader tcpClientStreamReader;
    private StreamWriter tcpClientStreamWriter;
    private IPEndPoint serverSideTcpClientEndPoint;

    public bool IsConnected => serverSideTcpClientEndPoint != null
                                && tcpClient != null
                                && tcpClientStream != null
                                && tcpClientStreamReader != null
                                && tcpClientStreamWriter != null;

    private IAudioSamplesAnalyzer audioSamplesAnalyzer;

    private Thread receiveDataThread;
    private Thread serverStillAliveCheckThread;

    private SongMeta songMeta;
    private double receivedPositionInSongInMillis = -1;
    private long unixTimeMillisecondsWhenReceivedPositionInSong;
    private int lastAnalyzedBeat;

    private bool HasPositionInSong => songMeta != null && receivedPositionInSongInMillis >= 0;

    private bool receivedStopRecordingMessage;
    private bool receivedStartRecordingMessage;

    private void Start()
    {
        UpdateAudioSamplesAnalyzer();
        settings.ObserveEveryValueChanged(it => it.MicProfile)
            .Subscribe(newValue => UpdateAudioSamplesAnalyzer());

        ResetPositionInSong();

        clientSideConnectRequestManager.ConnectEventStream.Subscribe(UpdateConnectionStatus);
        clientSideMicSampleRecorder.RecordingEventStream.Subscribe(HandleNewMicSamples);
        clientSideMicSampleRecorder.IsRecording.Subscribe(HandleRecordingStatusChanged);

        // Receive messages from server (i.e. from main game)
        receiveDataThread = new Thread(() =>
        {
            while (true)
            {
                try
                {
                    if (IsConnected)
                    {
                        while (tcpClientStream.DataAvailable)
                        {
                            ReadMessageFromServer();
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    CloseNetworkConnection();
                }

                Thread.Sleep(250);
            }
        });
        receiveDataThread.Start();

        serverStillAliveCheckThread = new Thread(() =>
        {
            while (true)
            {
                if (IsConnected)
                {
                    CheckServerStillAlive();
                }
                Thread.Sleep(1500);
            }
        });
        serverStillAliveCheckThread.Start();
    }

    private void CheckServerStillAlive()
    {
        try
        {
            // If there is new data available, then the client is still alive.
            if (!tcpClientStream.DataAvailable)
            {
                // Try to send something to the client.
                // If this fails with an Exception, then the connection has been lost and the client has to reconnect.
                SendMessageToServer(new StillAliveCheckDto());
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Failed sending data to server. Closing connection.");
            CloseNetworkConnection();
        }
    }

    private void Update()
    {
        if (receivedStopRecordingMessage)
        {
            receivedStopRecordingMessage = false;

            ResetPositionInSong();

            // Must be called from main thread.
            Debug.Log("Stopping recording because of message from server");
            clientSideMicSampleRecorder.StopRecording();
        }
        if (receivedStartRecordingMessage)
        {
            receivedStartRecordingMessage = false;

            // Must be called from main thread.
            Debug.Log("Starting recording because of message from server");
            clientSideMicSampleRecorder.StartRecording();
        }

        if (HasPositionInSong
            && unixTimeMillisecondsWhenReceivedPositionInSong + 30000 < TimeUtils.GetUnixTimeMilliseconds())
        {
            // Did not receive new position in song for some time. Probably not in sing scene anymore.
            ResetPositionInSong();
        }
    }

    private void UpdateAudioSamplesAnalyzer()
    {
        audioSamplesAnalyzer = AbstractMicPitchTracker.CreateAudioSamplesAnalyzer(EPitchDetectionAlgorithm.Dywa, GetMicProfileWithFinalSampleRate().SampleRate);
        audioSamplesAnalyzer.Enable();
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
        if (!IsConnected)
        {
            return;
        }

        // Do pitch detection
        if (HasPositionInSong)
        {
            AnalyzeMicSamplesCorrespondingToBeatsInSong(recordingEvent);
        }
        else
        {
            Debug.Log("No position in song, analyzing newest samples");
            AnalyzeNewestMicSamples(recordingEvent);
        }
    }

    private void AnalyzeMicSamplesCorrespondingToBeatsInSong(RecordingEvent recordingEvent)
    {
        // Check if can analyze new beat
        double estimatedPositionInSongInMillis = GetEstimatedPositionInSongInMillis();
        double positionInSongConsideringMicDelay = estimatedPositionInSongInMillis - settings.MicProfile.DelayInMillis;
        int currentBeatConsideringMicDelay = (int)BpmUtils.MillisecondInSongToBeat(songMeta, positionInSongConsideringMicDelay);
        // int currentBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, estimatedPositionInSongInMillis);
        // Debug.Log($"currentBeat: {currentBeat}, withDelay: {currentBeatConsideringMicDelay} (diff: {currentBeat - currentBeatConsideringMicDelay})");
        if (currentBeatConsideringMicDelay <= lastAnalyzedBeat)
        {
            return;
        }

        // Do not analyze more than 100 beats (might missed some beats while app was in background)
        int firstNextBeatToAnalyze = Math.Max(lastAnalyzedBeat + 1, currentBeatConsideringMicDelay - 100);
        // Debug.Log($"Analyzing beats from {nextBeatToAnalyze} to {currentBeat} ({currentBeat - lastAnalyzedBeat} beats, at frame {Time.frameCount}, at systime {TimeUtils.GetUnixTimeMilliseconds()})");

        List<BeatPitchEvent> beatPitchEvents = new();
        int loopCount = 0;
        int maxLoopCount = 100;
        for (int beat = firstNextBeatToAnalyze; beat <= currentBeatConsideringMicDelay; beat++)
        {
            PitchEvent pitchEvent = AnalyzeMicSamplesOfBeat(recordingEvent, beat);
            int midiNote = pitchEvent != null
                ? pitchEvent.MidiNote
                : -1;
            beatPitchEvents.Add(new BeatPitchEvent(midiNote, beat));
            // Debug.Log($"Analyzed beat {beat}: midiNote: {midiNote}");

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
        BeatPitchEventDto beatPitchEventDto = new BeatPitchEventDto(midiNote, -1);
        SendMessageToServer(new BeatPitchEventsDto(beatPitchEventDto));
    }

    private double GetEstimatedPositionInSongInMillis()
    {
        if (!HasPositionInSong)
        {
            return 0;
        }

        long currentUnixTimeMilliseconds = TimeUtils.GetUnixTimeMilliseconds();
        long durationSinceReceivedPositionInSongInMillis = currentUnixTimeMilliseconds - unixTimeMillisecondsWhenReceivedPositionInSong;
        double estimatedPositionInSongInMillis = receivedPositionInSongInMillis + durationSinceReceivedPositionInSongInMillis;
        return estimatedPositionInSongInMillis;
    }

    private PitchEvent AnalyzeMicSamplesOfBeat(RecordingEvent recordingEvent, int beat)
    {
        PitchEvent pitchEvent = AbstractMicPitchTracker.AnalyzeBeat(
            songMeta,
            beat,
            GetEstimatedPositionInSongInMillis(),
            GetMicProfileWithFinalSampleRate(),
            recordingEvent.MicSamples,
            audioSamplesAnalyzer);
        return pitchEvent;
    }

    private MicProfile GetMicProfileWithFinalSampleRate()
    {
        // The MicProfile in the settings may use a SampleRate of 0 for "best available".
        // The pitch detection algorithm needs the proper value.
        MicProfile micProfile = new MicProfile(settings.MicProfile);
        micProfile.SampleRate = ClientSideMicSampleRecorder.GetFinalSampleRate(settings.MicProfile.Name, settings.MicProfile.SampleRate);
        return micProfile;
    }

    private void SendMessageToServer(JsonSerializable jsonSerializable)
    {
        try
        {
            tcpClientStreamWriter.WriteLine(jsonSerializable.ToJson());
            tcpClientStreamWriter.Flush();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Failed to send pitch to server");
            CloseNetworkConnection();
        }
    }

    private void ReadMessageFromServer()
    {
        string receivedLine = tcpClientStreamReader.ReadLine();
        if (receivedLine.IsNullOrEmpty())
        {
            return;
        }

        receivedLine = receivedLine.Trim();
        if (!receivedLine.StartsWith("{")
            || !receivedLine.EndsWith("}"))
        {
            Debug.LogWarning($"Received invalid message from server: {receivedLine}");
            return;
        }

        HandleJsonMessageFromServer(receivedLine);
    }

    private void HandleJsonMessageFromServer(string json)
    {
        CompanionAppMessageDto companionAppMessageDto = null;
        try
        {
            companionAppMessageDto = JsonConverter.FromJson<CompanionAppMessageDto>(json);
        }
        catch (Exception e)
        {
            Debug.Log($"Exception while parsing message from server: {json}");
            Debug.LogException(e);
        }

        switch (companionAppMessageDto.MessageType)
        {
            case CompanionAppMessageType.StillAliveCheck:
                // Nothing to do. If the connection would not be still alive anymore, then this message would have failed already.
                return;
            case CompanionAppMessageType.PositionInSong:
                // Immediately send the response. It is used to measure the message delay.
                SendMessageToServer(new PositionInSongResponseDto());
                // Handle the message.
                ReceivedPositionInSong(JsonConverter.FromJson<PositionInSongDto>(json));
                return;
            case CompanionAppMessageType.MicProfile:
                SetMicProfile(JsonConverter.FromJson<MicProfileMessageDto>(json));
                return;
            case CompanionAppMessageType.StopRecording:
                // Must be called from main thread
                receivedStopRecordingMessage = true;
                return;
            case CompanionAppMessageType.StartRecording:
                // Must be called from main thread
                receivedStartRecordingMessage = true;
                return;
            default:
                Debug.Log($"Unknown MessageType {companionAppMessageDto.MessageType} in JSON from server: {json}");
                return;
        }
    }

    private void SetMicProfile(MicProfileMessageDto micProfileMessageDto)
    {
        Debug.Log($"Received new mic profile: {micProfileMessageDto.ToJson()}");

        MicProfile micProfile = new MicProfile(settings.MicProfile.Name);
        micProfile.Amplification = micProfileMessageDto.Amplification;
        micProfile.NoiseSuppression = micProfileMessageDto.NoiseSuppression;
        micProfile.SampleRate = micProfileMessageDto.SampleRate;
        micProfile.DelayInMillis = micProfileMessageDto.DelayInMillis;
        micProfile.Color = Colors.CreateColor(micProfileMessageDto.HexColor);

        settings.MicProfile = micProfile;
    }

    private void ReceivedPositionInSong(PositionInSongDto positionInSongDto)
    {
        double estimatedPositionInSongInMillis = GetEstimatedPositionInSongInMillis();
        double newReceivedPositionInSongInMillis = positionInSongDto.PositionInSongInMillis + positionInSongDto.MessageDelayInMillis;

        receivedPositionInSongInMillis = newReceivedPositionInSongInMillis;
        unixTimeMillisecondsWhenReceivedPositionInSong = TimeUtils.GetUnixTimeMilliseconds();
        songMeta = new SongMeta
        {
            Bpm = positionInSongDto.SongBpm,
            Gap = positionInSongDto.SongGap,
        };

        // If beats have been analyzed prematurely, then redo analysis.
        int currentBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, receivedPositionInSongInMillis);
        if (lastAnalyzedBeat > currentBeat)
        {
            lastAnalyzedBeat = currentBeat;
        }

        Debug.Log($"Received position in song (millis: {positionInSongDto.PositionInSongInMillis}, messageDelay: {positionInSongDto.MessageDelayInMillis}, offset: {newReceivedPositionInSongInMillis - estimatedPositionInSongInMillis}, lastAnalyzedBeat: {lastAnalyzedBeat})");
    }

    private void ResetPositionInSong()
    {
        Debug.Log("Resetting position in song");
        unixTimeMillisecondsWhenReceivedPositionInSong = -1;
        songMeta = null;
        receivedPositionInSongInMillis = -1;
        lastAnalyzedBeat = -1;
    }

    private void UpdateConnectionStatus(ConnectEvent connectEvent)
    {
        if (connectEvent.IsSuccess
            && connectEvent.MicrophonePort > 0
            && connectEvent.ServerIpEndPoint != null)
        {
            serverSideTcpClientEndPoint = new IPEndPoint(connectEvent.ServerIpEndPoint.Address, connectEvent.MicrophonePort);

            CloseNetworkConnection();
            try
            {
                tcpClient = new TcpClient();
                tcpClient.NoDelay = true;
                tcpClient.Connect(serverSideTcpClientEndPoint);
                tcpClientStream = tcpClient.GetStream();
                tcpClientStreamReader = new StreamReader(tcpClientStream);
                tcpClientStreamWriter = new StreamWriter(tcpClientStream);
                tcpClientStreamWriter.AutoFlush = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                CloseNetworkConnection();
            }
        }
        else
        {
            serverSideTcpClientEndPoint = null;
            clientSideMicSampleRecorder.StopRecording();

            // Already disconnected.
            // Do not try to call reconnect (i.e. disconnect then connect) because it would cause a stack overflow.
            CloseNetworkConnection();
        }
    }

    private void OnDestroy()
    {
        CloseNetworkConnection();
    }

    private void CloseNetworkConnection()
    {
        tcpClientStream?.Close();
        tcpClientStream = null;
        tcpClient?.Close();
        tcpClient = null;
    }
}
