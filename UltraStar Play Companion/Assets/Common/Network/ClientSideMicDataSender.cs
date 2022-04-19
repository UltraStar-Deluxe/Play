using System;
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
    private double receivedPositionInSongInMillis;
    private long systemTimeWhenReceivedPositionInSong;
    private int lastAnalyzedBeat;

    private bool HasPositionInSong => songMeta != null && receivedPositionInSongInMillis >= 0;

    private bool receivedStopRecordingMessage;

    private void Start()
    {
        UpdateAudioSamplesAnalyzer();
        clientSideMicSampleRecorder.SampleRate.Subscribe(_ => UpdateAudioSamplesAnalyzer());

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
                tcpClientStreamWriter.WriteLine(new StillAliveCheckDto().ToJson());
                tcpClientStreamWriter.Flush();
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

        if (HasPositionInSong
            && systemTimeWhenReceivedPositionInSong + 10000 < TimeUtils.GetSystemTimeInMillis())
        {
            // Did not receive new position in song for some time. Probably not in sing scene anymore.
            ResetPositionInSong();
        }
    }

    private void UpdateAudioSamplesAnalyzer()
    {
        audioSamplesAnalyzer = AbstractMicPitchTracker.CreateAudioSamplesAnalyzer(EPitchDetectionAlgorithm.Dywa, clientSideMicSampleRecorder.SampleRate.Value);
        audioSamplesAnalyzer.Enable();
    }

    private void HandleRecordingStatusChanged(bool isRecording)
    {
        if (!isRecording)
        {
            // Nothing to do.
            return;
        }

        if (HasPositionInSong)
        {
            // Analyze the following beats, not past beats.
            lastAnalyzedBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, GetEstimatedPositionInSongInMillis());
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
            AnalyzeNewestMicSamples(recordingEvent);
        }
    }

    private void AnalyzeMicSamplesCorrespondingToBeatsInSong(RecordingEvent recordingEvent)
    {
        // Check if can analyze new beat
        int currentBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, GetEstimatedPositionInSongInMillis());
        if (currentBeat <= lastAnalyzedBeat
            || GetEstimatedPositionInSongInMillis() < songMeta.Gap)
        {
            return;
        }

        // Do not analyze more than 100 beats (might missed some beats while app was in background)
        int nextBeatToAnalyze = Math.Max(lastAnalyzedBeat + 1, currentBeat - 100);
        Debug.Log($"Analyzing beats from {nextBeatToAnalyze} to {currentBeat} ({currentBeat - lastAnalyzedBeat} beats)");

        int loopCount = 0;
        int maxLoopCount = 100;
        for (int beat = nextBeatToAnalyze; beat <= currentBeat; beat++)
        {
            PitchEvent pitchEvent = AnalyzeMicSamplesOfBeat(recordingEvent, beat);
            int midiNote = pitchEvent != null
                ? pitchEvent.MidiNote
                : -1;
            Debug.Log($"Analyzed beat {beat}: midiNote: {midiNote}");
            SendPitchEventToServer(new BeatPitchEvent(midiNote, beat));

            loopCount++;
            if (loopCount > maxLoopCount)
            {
                // Emergency exit
                Debug.LogWarning($"Took emergency exit out of loop. Analyzed {maxLoopCount} beats and still not finished?");
            }
        }
        lastAnalyzedBeat = currentBeat;
    }

    private void AnalyzeNewestMicSamples(RecordingEvent recordingEvent)
    {
        PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(
            recordingEvent.MicSamples,
            recordingEvent.NewSamplesStartIndex,
            recordingEvent.NewSamplesEndIndex,
            clientSideMicSampleRecorder.MicProfile);
        int midiNote = pitchEvent != null
            ? pitchEvent.MidiNote
            : -1;
        SendPitchEventToServer(new BeatPitchEvent(midiNote, -1));
    }


    private double GetEstimatedPositionInSongInMillis()
    {
        if (!HasPositionInSong)
        {
            return -1;
        }

        long currentSystemTimeMillis = TimeUtils.GetSystemTimeInMillis();
        long durationSinceReceivedPositionInSongInMillis = currentSystemTimeMillis - systemTimeWhenReceivedPositionInSong;
        double estimatedPositionInSongInMillis = receivedPositionInSongInMillis + durationSinceReceivedPositionInSongInMillis;
        return estimatedPositionInSongInMillis;
    }

    private PitchEvent AnalyzeMicSamplesOfBeat(RecordingEvent recordingEvent, int beat)
    {
        PitchEvent pitchEvent = AbstractMicPitchTracker.AnalyzeBeat(
            songMeta,
            beat,
            receivedPositionInSongInMillis,
            clientSideMicSampleRecorder.MicProfile,
            recordingEvent.MicSamples,
            audioSamplesAnalyzer);
        return pitchEvent;
    }

    private void SendPitchEventToServer(BeatPitchEvent pitchEvent)
    {
        try
        {
            BeatPitchEventDto beatPitchEventDto = pitchEvent != null
                ? new BeatPitchEventDto(pitchEvent.MidiNote, pitchEvent.Beat)
                : new BeatPitchEventDto(-1, -1);
            Debug.Log($"Sending pitch to server (beat: {beatPitchEventDto.Beat}, midiNote: {beatPitchEventDto.MidiNote})");
            tcpClientStreamWriter.WriteLine(beatPitchEventDto.ToJson());
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
        CompanionAppMessageDto companionAppMessageDto = JsonConverter.FromJson<CompanionAppMessageDto>(json);
        switch (companionAppMessageDto.MessageType)
        {
            case CompanionAppMessageType.StillAliveCheck:
                // Nothing to do. If the connection would not be still alive anymore, then this message would have failed already.
                return;
            case CompanionAppMessageType.PositionInSong:
                SetPositionInSong(JsonConverter.FromJson<PositionInSongDto>(json));
                return;
            case CompanionAppMessageType.StopRecording:
                // Must be called from main thread
                receivedStopRecordingMessage = true;
                return;
            default:
                Debug.Log($"Unknown MessageType {companionAppMessageDto.MessageType} in JSON from server: {json}");
                return;
        }
    }

    private void SetPositionInSong(PositionInSongDto positionInSongDto)
    {
        Debug.Log($"Received position in song {positionInSongDto.PositionInSongInMillis} (offset: {positionInSongDto.PositionInSongInMillis - GetEstimatedPositionInSongInMillis()})");
        if (positionInSongDto.PositionInSongInMillis < receivedPositionInSongInMillis)
        {
            // Jump back in song (possibly restart)
            lastAnalyzedBeat = 0;
        }
        systemTimeWhenReceivedPositionInSong = TimeUtils.GetSystemTimeInMillis();
        receivedPositionInSongInMillis = positionInSongDto.PositionInSongInMillis;
        songMeta = new SongMeta
        {
            Bpm = positionInSongDto.SongBpm,
            Gap = positionInSongDto.SongGap,
        };
    }

    private void ResetPositionInSong()
    {
        Debug.Log("Resetting position in song");
        systemTimeWhenReceivedPositionInSong = -1;
        songMeta = null;
        receivedPositionInSongInMillis = 0;
        lastAnalyzedBeat = 0;
    }

    private void UpdateConnectionStatus(ConnectEvent connectEvent)
    {
        if (connectEvent.IsSuccess
            && connectEvent.MicrophonePort > 0
            && connectEvent.ServerIpEndPoint != null)
        {
            serverSideTcpClientEndPoint = new IPEndPoint(connectEvent.ServerIpEndPoint.Address, connectEvent.MicrophonePort);
            if (connectEvent.MicrophoneSampleRate > 0
                && connectEvent.MicrophoneSampleRate != settings.SampleRate)
            {
                Debug.Log($"Received new sample rate: {settings.SampleRate}");
                settings.SampleRate = connectEvent.MicrophoneSampleRate;
                if (clientSideMicSampleRecorder.SampleRate.Value != settings.SampleRate)
                {
                    clientSideMicSampleRecorder.SetSampleRate(settings.SampleRate);
                    // Try again with new SampleRate received from main game.
                    CloseNetworkConnection();
                    return;
                }
            }

            CloseNetworkConnection();
            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(serverSideTcpClientEndPoint);
                tcpClientStream = tcpClient.GetStream();
                tcpClientStreamReader = new StreamReader(tcpClientStream);
                tcpClientStreamWriter = new StreamWriter(tcpClientStream);
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
