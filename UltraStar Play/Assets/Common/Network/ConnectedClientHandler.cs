using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class ConnectedClientHandler : IDisposable
{
    private static readonly object micSampleBufferReadWriteLock = new object();

    public IPEndPoint ClientIpEndPoint { get; private set; }
    public string ClientName { get; private set; }
    public UdpClient MicrophoneUdpClient { get; private set; }

    public string ClientId => ClientConnectionManager.GetClientId(ClientIpEndPoint);
    public int SampleRateHz => micSampleBuffer.Length;
    
    private bool isDisposed;

    private int newSamplesInMicBuffer;
    private int writePositionInMicBuffer;
    private int readPositionInMicBuffer;
    private readonly float[] micSampleBuffer;
    
    public ConnectedClientHandler(IPEndPoint clientIpEndPoint, string clientName, int microphoneSampleRate)
    {
        ClientIpEndPoint = clientIpEndPoint;
        ClientName = clientName;
        if (microphoneSampleRate <= 0)
        {
            Debug.LogWarning("Attempt to create ConnectedClientHandler without microphoneSampleRate");
            return;
        }

        micSampleBuffer = new float[microphoneSampleRate];
        MicrophoneUdpClient = new UdpClient(0);
        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            while (!isDisposed)
            {
                AcceptMicrophoneData();
            }
        });
    }

    private void AcceptMicrophoneData()
    {
        try
        {
            IPEndPoint serverIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] receivedBytes = MicrophoneUdpClient.Receive(ref serverIpEndPoint);
            FillMicBuffer(receivedBytes);
        }
        catch (Exception e)
        {
            if (e is SocketException se
                && se.SocketErrorCode == SocketError.Interrupted
                && isDisposed)
            {
                // Dont log error when closing the socket has interrupted the wait for requests.
                return;
            }
            Debug.LogException(e);
        }
    }

    private void FillMicBuffer(byte[] receivedBytes)
    {
        // Copy from byte array to float array. Note that in a float there are sizeof(float) bytes.
        float[] receivedSamples = new float[receivedBytes.Length / sizeof(float)];
        Buffer.BlockCopy(
            receivedBytes, 0,
            receivedSamples, 0,
            receivedBytes.Length);

        if (receivedSamples.Length > micSampleBuffer.Length)
        {
            // This should never happen.
            // Mic buffer should be at least 22050 samples,
            // which is 22050*4 bytes (without any compression),
            // which is far more than a single datagram can transmit.
            Debug.LogError("Received mic data does not fit into mic buffer.");
            return;
        }

        // Write into circular buffer
        lock (micSampleBufferReadWriteLock)
        {
            foreach (float sample in receivedSamples)
            {
                micSampleBuffer[writePositionInMicBuffer] = sample;
                writePositionInMicBuffer = (writePositionInMicBuffer + 1) % micSampleBuffer.Length;
            }
        
            newSamplesInMicBuffer += receivedSamples.Length;
            if (newSamplesInMicBuffer > micSampleBuffer.Length)
            {
                newSamplesInMicBuffer = micSampleBuffer.Length;
            }
        }
        
        // DateTime now = DateTime.Now; 
        // Debug.Log($"Received datagram: {receivedBytes.Length} bytes at {now}:{now.Millisecond}");
    }

    public void Dispose()
    {
        isDisposed = true;
        MicrophoneUdpClient?.Close();
    }

    /**
     * Writes the unread samples into the target buffer.
     * Thereby, the newest samples are written to the highest index and old samples are shifted to the left.
     * This corresponds to the behaviour of Unity's Microphone.GetData.
     *
     * Returns the number of new samples that have been written to the target buffer.
     */
    public int GetNewMicSamples(float[] targetArray)
    {
        if (targetArray.Length < newSamplesInMicBuffer)
        {
            throw new UnityException("Unread samples do not fit into target array");
        }

        if (newSamplesInMicBuffer == 0)
        {
            return 0;
        }
        
        lock (micSampleBufferReadWriteLock)
        {
            int newSamplesCount = newSamplesInMicBuffer;
            
            // Shift old samples that will not change to the left.
            int oldSampleCountInTargetArray = targetArray.Length - newSamplesInMicBuffer;
            for (int i = 0; i < oldSampleCountInTargetArray; i++)
            {
                targetArray[i] = targetArray[i + newSamplesInMicBuffer];
            }
            
            // Write new samples.
            for (int i = oldSampleCountInTargetArray; i < oldSampleCountInTargetArray + newSamplesInMicBuffer; i++)
            {
                targetArray[i] = micSampleBuffer[readPositionInMicBuffer];
                readPositionInMicBuffer = (readPositionInMicBuffer + 1) % micSampleBuffer.Length;
            }
            newSamplesInMicBuffer = 0;

            return newSamplesCount;
        }
    }
}
