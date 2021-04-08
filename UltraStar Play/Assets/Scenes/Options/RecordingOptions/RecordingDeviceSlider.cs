using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class RecordingDeviceSlider : TextItemSlider<MicProfile>, INeedInjection
{
    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    protected override void Awake()
    {
        base.Awake();
        UpdateItems();
    }

    protected override void Start()
    {
        base.Start();
        serverSideConnectRequestManager.ClientConnectedEventStream
            .Subscribe(UpdateMicProfileNames)
            .AddTo(gameObject);
    }

    private void UpdateMicProfileNames(ClientConnectionEvent clientConnectionEvent)
    {
        if (clientConnectionEvent.IsConnected)
        {
            Items.ForEach(micProfile => UpdateMicProfileName(clientConnectionEvent, micProfile));
        }
        uiItemText.text = GetDisplayString(SelectedItem);
    }

    private void UpdateMicProfileName(ClientConnectionEvent clientConnectionEvent, MicProfile micProfile)
    {
        if (micProfile.IsInputFromConnectedClient
            && micProfile.ConnectedClientId == clientConnectionEvent.ConnectedClientHandler.ClientId)
        {
            micProfile.Name = clientConnectionEvent.ConnectedClientHandler.ClientName;

            if (SelectedItem == micProfile)
            {
                uiItemText.text = micProfile.Name;
            }
        }
    }

    protected override string GetDisplayString(MicProfile micProfile)
    {
        if (micProfile == null)
        {
            return "";
        }
        else
        {
            return micProfile.IsInputFromConnectedClient && micProfile.IsConnected
                ? micProfile.Name + $"\n({micProfile.ConnectedClientId})"
                : micProfile.Name;
        }
    }

    public void UpdateItems()
    {
        // Create list of connected and loaded microphones without duplicates.
        // A loaded microphone might have been created with hardware that is not connected now.
        List<string> connectedMicNames = Microphone.devices.ToList();
        List<MicProfile> loadedMicProfiles = SettingsManager.Instance.Settings.MicProfiles;
        List<MicProfile> micProfiles = new List<MicProfile>(loadedMicProfiles);
        List<ConnectedClientHandler> connectedClientHandlers = ServerSideConnectRequestManager.GetConnectedClientHandlers();

        // Create mic profiles for connected microphones that are not yet in the list
        foreach (string connectedMicName in connectedMicNames)
        {
            bool alreadyInList = micProfiles.AnyMatch(it => it.Name == connectedMicName && !it.IsInputFromConnectedClient);
            if (!alreadyInList)
            {
                MicProfile micProfile = new MicProfile(connectedMicName);
                micProfiles.Add(micProfile);
            }
        }
        
        // Create mic profiles for connected companion apps that are not yet in the list
        foreach (ConnectedClientHandler connectedClientHandler in connectedClientHandlers)
        {
            bool alreadyInList = micProfiles.AnyMatch(it => it.ConnectedClientId == connectedClientHandler.ClientId && it.IsInputFromConnectedClient);
            if (!alreadyInList)
            {
                MicProfile micProfile = new MicProfile(connectedClientHandler.ClientName, connectedClientHandler.ClientId);
                micProfiles.Add(micProfile);
            }
        }

        Items = micProfiles;
    }
}
