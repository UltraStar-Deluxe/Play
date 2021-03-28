using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class ConnectedClientHandler : IDisposable
{
    private static readonly object micSampleBufferReadWriteLock = new object();

    public IPEndPoint ClientIpEndPoint { get; private set; }
    public string ClientName { get; private set; }
    public TcpListener MicTcpListener { get; private set; }
    public string ClientId => ServerSideConnectRequestManager.GetClientId(ClientIpEndPoint);
    public int SampleRateHz => micSampleBuffer.Length;
    
    private bool isDisposed;

    private TcpClient micTcpClient;
    private NetworkStream micTcpClientStream;
    
    private int newSamplesInMicBuffer;
    private int writePositionInMicBuffer;
    public readonly float[] micSampleBuffer;

    private readonly byte[] receivedByteBuffer;
    private readonly byte[] stillAliveRequestByteArray;

    private readonly ServerSideConnectRequestManager serverSideConnectRequestManager;
    
    public ConnectedClientHandler(ServerSideConnectRequestManager serverSideConnectRequestManager, IPEndPoint clientIpEndPoint, string clientName, int microphoneSampleRate)
    {
        this.serverSideConnectRequestManager = serverSideConnectRequestManager;
        ClientIpEndPoint = clientIpEndPoint;
        ClientName = clientName;
        if (microphoneSampleRate <= 0)
        {
            Debug.LogWarning("Attempt to create ConnectedClientHandler without microphoneSampleRate");
            return;
        }

        micSampleBuffer = new float[microphoneSampleRate];

        receivedByteBuffer = new byte[2048];
        stillAliveRequestByteArray = new byte[1];
        
        MicTcpListener = new TcpListener(IPAddress.Any, 0);
        MicTcpListener.Start();
        
        Debug.Log($"Started TcpListener on port {MicTcpListener.GetPort()} to receive mic samples at {microphoneSampleRate} Hz");
        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            while (!isDisposed)
            {
                if (micTcpClient == null)
                {
                    micTcpClient = MicTcpListener.AcceptTcpClient();
                    micTcpClientStream = micTcpClient.GetStream();
                }
                
                if (CheckClientStillAlive())
                {
                    AcceptMicrophoneData();
                }
                Thread.Sleep(10);
            }
        });
    }

    private bool CheckClientStillAlive()
    {
        try
        {
            // If there is new data available, then the client is still alive.
            if (!micTcpClientStream.DataAvailable)
            {
                // Try to send something to the client.
                // If this fails with an Exception, then the connection has been lost and the client has to reconnect.
                micTcpClientStream.Write(stillAliveRequestByteArray, 0, stillAliveRequestByteArray.Length);
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Failed sending data to client. Removing ConnectedClientHandler.");
            serverSideConnectRequestManager.RemoveConnectedClientHandler(this);
            return false;
        }
    }

    private void AcceptMicrophoneData()
    {
        try
        {
            // Loop to receive all the data sent by the client.
            int receivedByteCount;
            while (micTcpClientStream.DataAvailable
                   && (receivedByteCount = micTcpClientStream.Read(receivedByteBuffer, 0, receivedByteBuffer.Length)) > 0)
            {
                FillMicBuffer(receivedByteBuffer, receivedByteCount);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Failed receiving data from client. Removing ConnectedClientHandler.");
            serverSideConnectRequestManager.RemoveConnectedClientHandler(this);
        }
    }

    private void FillMicBuffer(byte[] receivedBytes, int receivedByteCount)
    {
        // Write into circular buffer
        lock (micSampleBufferReadWriteLock)
        {
            // Copy from byte array to float array. Note that in a float there are sizeof(float) bytes.
            float[] receivedSamples = new float[(int)Math.Ceiling((double)receivedByteCount / sizeof(float))];
            Buffer.BlockCopy(
                receivedBytes, 0,
                receivedSamples, 0,
                receivedByteCount);

            if (receivedSamples.Length > micSampleBuffer.Length)
            {
                Debug.LogError("Received mic data does not fit into mic buffer.");
                return;
            }

            foreach (float sample in receivedSamples)
            {
                micSampleBuffer[writePositionInMicBuffer] = sample;
                int lastWritePositionInMicBuffer = writePositionInMicBuffer;
                writePositionInMicBuffer = (writePositionInMicBuffer + 1) % micSampleBuffer.Length;
                if (writePositionInMicBuffer != (lastWritePositionInMicBuffer + 1))
                {
                    Debug.Log("Wrapped write pos in mic buffer: " + writePositionInMicBuffer);
                }
            }
        
            newSamplesInMicBuffer += receivedSamples.Length;
            if (newSamplesInMicBuffer > micSampleBuffer.Length)
            {
                newSamplesInMicBuffer = micSampleBuffer.Length;
            }
            DateTime now = DateTime.Now; 
            Debug.Log($"Received data: {receivedByteCount} bytes ({receivedSamples.Length} samples) at {now}:{now.Millisecond}");
        }
    }

    public void Dispose()
    {
        isDisposed = true;
        micTcpClientStream?.Close();
        micTcpClient?.Close();
        MicTcpListener?.Stop();
    }

    /**
     * Fills the target buffer with the newest samples.
     * Thereby, the newest samples are written to the highest index.
     * This corresponds to the behaviour of Unity's Microphone.GetData.
     *
     * Returns the number of new samples since this method has been called the last time.
     */
    public int GetNewMicSamples(float[] targetSampleBuffer)
    {
        lock (micSampleBufferReadWriteLock)
        {
            if (targetSampleBuffer.Length < newSamplesInMicBuffer)
            {
                throw new UnityException("Unread samples do not fit into target array");
            }

            for (int i = 0; i < targetSampleBuffer.Length; i++)
            {
                targetSampleBuffer[i] = micSampleBuffer[NumberUtils.Mod(i + writePositionInMicBuffer - targetSampleBuffer.Length, micSampleBuffer.Length)];
            }

            int newSamplesCount = newSamplesInMicBuffer;
            newSamplesInMicBuffer = 0;
            return newSamplesCount;
        }
    }
}
