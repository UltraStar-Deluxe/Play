using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class ConnectedClientHandler : IDisposable
{
    public string ClientId { get; private set; }
    public UdpClient MicrophoneUdpClient { get; private set; }

    private bool isDisposed;

    private int unreadBytesInMicBuffer;
    private int writePositionInMicBuffer;
    private byte[] micBuffer;
    
    public ConnectedClientHandler(string clientId, int microphoneSampleRate)
    {
        ClientId = clientId;
        if (microphoneSampleRate <= 0)
        {
            Debug.LogWarning("Attempt to create ConnectedClientHandler without microphoneSampleRate");
            return;
        }

        micBuffer = new byte[microphoneSampleRate];
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
            Debug.Log($"Received {receivedBytes.Length} mic samples from client");
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
        if (receivedBytes.Length >= micBuffer.Length)
        {
            // Take the portion of the received bytes that fit into the micBuffer.
            for (int i = 0; i < micBuffer.Length; i++)
            {
                writePositionInMicBuffer = (writePositionInMicBuffer + 1) % micBuffer.Length;
                micBuffer[writePositionInMicBuffer] = receivedBytes[i + (receivedBytes.Length - micBuffer.Length)];
            }

            unreadBytesInMicBuffer = micBuffer.Length;
        }
        else
        {
            for (int i = 0; i < micBuffer.Length && i < receivedBytes.Length; i++)
            {
                writePositionInMicBuffer = (writePositionInMicBuffer + 1) % micBuffer.Length;
                micBuffer[writePositionInMicBuffer] = receivedBytes[i];
            }

            unreadBytesInMicBuffer = (unreadBytesInMicBuffer + receivedBytes.Length);
            if (unreadBytesInMicBuffer > micBuffer.Length)
            {
                unreadBytesInMicBuffer = micBuffer.Length;
            }
        }
    }

    public void Dispose()
    {
        isDisposed = true;
        MicrophoneUdpClient?.Close();
    }
}
