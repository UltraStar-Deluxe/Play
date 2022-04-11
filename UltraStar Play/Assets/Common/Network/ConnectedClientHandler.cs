using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class ConnectedClientHandler : IConnectedClientHandler
{
    public IPEndPoint ClientIpEndPoint { get; private set; }
    public string ClientName { get; private set; }
    public string ClientId { get; private set; }
    public TcpListener MicTcpListener { get; private set; }
    public int SampleRateHz => 44100;

    private bool isDisposed;

    private TcpClient tcpClient;
    private NetworkStream tcpClientStream;
    private StreamReader tcpClientStreamReader;
    private StreamWriter tcpClientStreamWriter;

    private int newSamplesInMicBuffer;
    private int writePositionInMicBuffer;

    private readonly byte[] stillAliveRequestByteArray;

    private readonly ServerSideConnectRequestManager serverSideConnectRequestManager;

    private readonly Thread receiveDataThread;
    private readonly Thread clientStillAliveCheckThread;
    
    public ConnectedClientHandler(ServerSideConnectRequestManager serverSideConnectRequestManager, IPEndPoint clientIpEndPoint, string clientName, string clientId, int microphoneSampleRate)
    {
        this.serverSideConnectRequestManager = serverSideConnectRequestManager;
        ClientIpEndPoint = clientIpEndPoint;
        ClientName = clientName;
        ClientId = clientId;
        if (ClientId.IsNullOrEmpty())
        {
            throw new ArgumentException("Attempt to create ConnectedClientHandler without ClientId");
        }
        if (microphoneSampleRate <= 0)
        {
            throw new ArgumentException("Attempt to create ConnectedClientHandler without microphoneSampleRate");
        }

        stillAliveRequestByteArray = new byte[1];
        
        MicTcpListener = new TcpListener(IPAddress.Any, 0);
        MicTcpListener.Start();
        
        Debug.Log($"Started TcpListener on port {MicTcpListener.GetPort()} to receive mic samples at {microphoneSampleRate} Hz");
        receiveDataThread = new Thread(() =>
        {
            while (!isDisposed)
            {
                try
                {
                    if (tcpClient == null)
                    {
                        tcpClient = MicTcpListener.AcceptTcpClient();
                        tcpClientStream = tcpClient.GetStream();
                        tcpClientStreamReader = new StreamReader(tcpClientStream);
                        tcpClientStreamWriter = new StreamWriter(tcpClientStream);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogError("Error when accepting TcpClient for mic input. Closing TcpListener.");
                    this.serverSideConnectRequestManager.RemoveConnectedClientHandler(this);
                    return;
                }

                if (tcpClientStream.DataAvailable)
                {
                    AcceptClientMessages();
                }
                else
                {
                    Thread.Sleep(250);
                }
            }
        });
        receiveDataThread.Start();
        
        clientStillAliveCheckThread = new Thread(() =>
        {
            while (!isDisposed)
            {
                if (tcpClient != null
                    && tcpClientStream != null)
                {
                    CheckClientStillAlive();
                }
                Thread.Sleep(1500);
            }
        });
        clientStillAliveCheckThread.Start();
    }
    
    private void CheckClientStillAlive()
    {
        try
        {
            // If there is new data available, then the client is still alive.
            if (!tcpClientStream.DataAvailable)
            {
                // Try to send something to the client.
                // If this fails with an Exception, then the connection has been lost and the client has to reconnect.
                tcpClientStreamWriter.WriteLine("{\"message-type\": \"still-alive-check\"}");
                tcpClientStreamWriter.Flush();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Failed sending data to client. Removing ConnectedClientHandler.");
            serverSideConnectRequestManager.RemoveConnectedClientHandler(this);
        }
    }

    private void AcceptClientMessages()
    {
        if (!tcpClientStream.DataAvailable)
        {
            return;
        }

        try
        {
            string line = tcpClientStreamReader.ReadLine();
            if (line.IsNullOrEmpty())
            {
                return;
            }

            line = line.Trim();
            if (!line.StartsWith("{")
                || !line.EndsWith("}"))
            {
                Debug.LogWarning("Received invalid JSON from client.");
                return;
            }

            HandleClientJsonMessage(line);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Failed receiving data from client. Removing ConnectedClientHandler.");
            serverSideConnectRequestManager.RemoveConnectedClientHandler(this);
        }
    }

    private void HandleClientJsonMessage(string json)
    {
        Debug.Log($"Received JSON from client: {json}");
    }

    public void Dispose()
    {
        isDisposed = true;
        tcpClientStream?.Close();
        tcpClient?.Close();
        MicTcpListener?.Stop();
    }

    public int GetNewMicSamples(float[] targetSampleBuffer)
    {
        // TODO: Remove
        return 0;
    }
}
