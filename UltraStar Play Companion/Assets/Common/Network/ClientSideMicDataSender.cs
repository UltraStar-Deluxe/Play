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
    private ClientSideConnectRequestManager clientSideConnectRequestManager;

    [Inject]
    private ClientSideMicSampleRecorder clientSideMicSampleRecorder;

    [Inject]
    private Settings settings;
    
    private TcpClient tcpClient;
    private NetworkStream tcpClientStream;
    private StreamReader tcpClientStreamReader;
    private StreamWriter tcpClientStreamWriter;
    private IPEndPoint serverSideTcpClientEndPoint;

    private IAudioSamplesAnalyzer audioSamplesAnalyzer;

    private Thread receiveDataThread;

    private long lastSystemTimeWhenSetPositionInSong;
    private long lastSystemTimeWhenCallingUpdate;

    private SongMeta songMeta;
    private double positionInSongInMillis;
    private int lastAnalyzedBeat;

    private void Start()
    {
        UpdateAudioSamplesAnalyzer();
        clientSideMicSampleRecorder.SampleRate.Subscribe(_ => UpdateAudioSamplesAnalyzer());

        clientSideConnectRequestManager.ConnectEventStream.Subscribe(UpdateConnectionStatus);
        clientSideMicSampleRecorder.RecordingEventStream.Subscribe(HandleNewMicSamples);

        // Receive messages from server (i.e. from main game)
        receiveDataThread = new Thread(() =>
        {
            while (true)
            {
                try
                {
                    if (serverSideTcpClientEndPoint != null
                        && tcpClient != null
                        && tcpClientStream != null)
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
    }

    private void Update()
    {
        if (songMeta != null)
        {
            long currentSystemTimeMillis = TimeUtils.GetSystemTimeInMillis();
            long deltaSystemTimeMillis = currentSystemTimeMillis - lastSystemTimeWhenCallingUpdate;
            positionInSongInMillis += deltaSystemTimeMillis;
            lastSystemTimeWhenCallingUpdate = currentSystemTimeMillis;
        }

        if (lastSystemTimeWhenSetPositionInSong + 10000 < TimeUtils.GetSystemTimeInMillis())
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

    private void HandleNewMicSamples(RecordingEvent recordingEvent)
    {
        if (serverSideTcpClientEndPoint == null
            || tcpClient == null
            || tcpClientStream == null)
        {
            return;
        }

        // Do pitch detection
        if (songMeta == null
            || positionInSongInMillis < 0)
        {
            // Analyze the newest samples
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
        else
        {
            // Check if can analyze now completed beat
            int currentBeat = (int)BpmUtils.MillisecondInSongToBeat(songMeta, positionInSongInMillis);
            if (currentBeat <= lastAnalyzedBeat)
            {
                return;
            }

            Debug.Log($"Analyzing beats from {lastAnalyzedBeat + 1} to {currentBeat}");
            int loopCount = 0;
            int maxLoopCount = 100;
            for (int beat = lastAnalyzedBeat + 1; beat <= currentBeat; beat++)
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
    }

    private PitchEvent AnalyzeMicSamplesOfBeat(RecordingEvent recordingEvent, int beat)
    {
        PitchEvent pitchEvent = AbstractMicPitchTracker.AnalyzeBeat(
            songMeta,
            beat,
            positionInSongInMillis,
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
            Debug.LogError($"Failed to send message");
            clientSideConnectRequestManager.CloseConnectionAndReconnect();
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
            default:
                Debug.Log($"Unknown MessageType {companionAppMessageDto.MessageType} in JSON from server: {json}");
                return;
        }
    }

    private void SetPositionInSong(PositionInSongDto positionInSongDto)
    {
        Debug.Log($"Setting position in song to {positionInSongDto.PositionInSongInMillis} (offset: {positionInSongDto.PositionInSongInMillis - positionInSongInMillis})");
        lastSystemTimeWhenSetPositionInSong = TimeUtils.GetSystemTimeInMillis();
        positionInSongInMillis = positionInSongDto.PositionInSongInMillis;
        songMeta = new SongMeta
        {
            Bpm = positionInSongDto.SongBpm,
            Gap = positionInSongDto.SongGap,
        };
    }

    private void ResetPositionInSong()
    {
        Debug.Log("Resetting position in song");
        lastSystemTimeWhenSetPositionInSong = TimeUtils.GetSystemTimeInMillis();
        songMeta = null;
        positionInSongInMillis = 0;
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
            CloseNetworkConnection();
            serverSideTcpClientEndPoint = null;
            clientSideMicSampleRecorder.StopRecording();
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
