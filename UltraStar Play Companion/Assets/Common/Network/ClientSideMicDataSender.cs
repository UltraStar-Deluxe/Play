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

    private MicProfile micProfile;
    private IAudioSamplesAnalyzer audioSamplesAnalyzer;

    private Thread receiveDataThread;

    private float lastSendMessageTimeInSeconds;

    private void Start()
    {
        UpdateMicProfile();
        clientSideMicSampleRecorder.SampleRate.Subscribe(_ => UpdateMicProfile());

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
                            ReadServerMessage();
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

    private void UpdateMicProfile()
    {
        audioSamplesAnalyzer = AbstractMicPitchTracker.CreateAudioSamplesAnalyzer(EPitchDetectionAlgorithm.Dywa, clientSideMicSampleRecorder.SampleRate.Value);
        audioSamplesAnalyzer.Enable();
    }

    private void HandleNewMicSamples(RecordingEvent recordingEvent)
    {
        if (serverSideTcpClientEndPoint != null
            && tcpClient != null
            && tcpClientStream != null)
        {
            try
            {
                // TODO: Perform pitch detection on beats when song position is set
                PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(
                    recordingEvent.MicSamples,
                    recordingEvent.NewSamplesStartIndex,
                    recordingEvent.NewSamplesEndIndex,
                    clientSideMicSampleRecorder.MicProfile);

                if (pitchEvent == null)
                {
                    // Debug.Log($"Send empty pitchEvent");
                    tcpClientStreamWriter.WriteLine(new BeatPitchEventDto
                    {
                        Beat = -1,
                        MidiNote = -1,
                    }.ToJson());
                }
                else
                {
                    // Debug.Log($"Send pitchEvent: {pitchEvent.MidiNote}");
                    tcpClientStreamWriter.WriteLine(new BeatPitchEventDto
                    {
                        Beat = -1,
                        MidiNote = pitchEvent.MidiNote,
                    }.ToJson());
                }
                tcpClientStreamWriter.Flush();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Failed to send message");
                clientSideConnectRequestManager.CloseConnectionAndReconnect();
            }
        }
    }

    private void ReadServerMessage()
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

        HandleServerJsonMessage(receivedLine);
    }

    private void HandleServerJsonMessage(string json)
    {
        CompanionAppMessageDto companionAppMessageDto = JsonConverter.FromJson<CompanionAppMessageDto>(json);
        switch (companionAppMessageDto.MessageType)
        {
            case CompanionAppMessageType.StillAliveCheck:
                // Nothing to do. If the connection would not be still alive anymore, then this message would have failed already.
                return;
            case CompanionAppMessageType.PositionInSong:
                UpdatePositionInSong(JsonConverter.FromJson<PositionInSongDto>(json));
                return;
            default:
                Debug.Log($"Unknown MessageType {companionAppMessageDto.MessageType} in JSON from server: {json}");
                return;
        }
    }

    private void UpdatePositionInSong(PositionInSongDto positionInSongDto)
    {
        // TODO: Consider position in song, BPM, and GAP to do pitch detection on beats.
    }

    private void SendMicData(RecordingEvent recordingEvent)
    {
        if (recordingEvent.NewSampleCount >= clientSideMicSampleRecorder.SampleRate.Value - 1)
        {
            Debug.LogError("Attempt to send complete mic buffer at once");
            return;
        }
        // Copy from float array to byte array. Note that in a float there are sizeof(float) bytes.
        byte[] newByteData = new byte[recordingEvent.NewSampleCount * sizeof(float)];
        Buffer.BlockCopy(
            recordingEvent.MicSamples, recordingEvent.NewSamplesStartIndex * sizeof(float),
            newByteData, 0,
            recordingEvent.NewSampleCount * sizeof(float));

        try
        {
            // DateTime now = DateTime.Now;
            // Debug.Log($"Send data: {newByteData.Length} bytes ({recordingEvent.NewSampleCount} samples) at {now}:{now.Millisecond}");
            tcpClientStream.Write(newByteData, 0, newByteData.Length);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Failed sending mic data: {newByteData.Length} bytes ({recordingEvent.NewSampleCount} samples)");
            clientSideConnectRequestManager.CloseConnectionAndReconnect();
        }
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
