using System;
using System.Collections.Generic;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerMicPitchOnlineMultiplayerControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private OnlineMultiplayerManager onlineMultiplayerManager;

    [Inject]
    private PlayerMicPitchTracker playerMicPitchTracker;

    [Inject]
    private PlayerProfile playerProfile;

    [Inject]
    private PlayerControl playerControl;

    [Inject]
    private Settings settings;

    private readonly List<IDisposable> disposables = new();

    private readonly HashSet<int> beatsSentToRemote = new();

    public void OnInjectionFinished()
    {
        if (!onlineMultiplayerManager.IsOnlineGame)
        {
            return;
        }

        if (playerProfile is LobbyMemberPlayerProfile
            && playerProfile != onlineMultiplayerManager.OwnLobbyMemberPlayerProfile)
        {
            // Cannot record mic samples for other lobby members
            playerMicPitchTracker.RecordNotes = false;

            // Receive pitch for this player via message from peer
            disposables.Add(onlineMultiplayerManager.MessagingControl.RegisterNamedMessageHandler(
                GetBeatAnalyzedEventMessageName(),
                message => OnBeatAnalyzedEventMessage(message)));
        }
        else
        {
            playerMicPitchTracker.BeatAnalyzedEventStream
                .Subscribe(evt =>
                {
                    beatsSentToRemote.Add(evt.Beat);
                    onlineMultiplayerManager.MessagingControl.SendNamedMessageToClients(
                        GetBeatAnalyzedEventMessageName(),
                        CreateBeatAnalyzedEventFastBufferWriter(evt),
                        onlineMultiplayerManager.OtherLobbyMembersUnityNetcodeClientIds,
                        settings.BeatAnalyzedEventNetworkDelivery.ToUnityNetworkDelivery());
                });
        }
    }

    private string GetBeatAnalyzedEventMessageName()
    {
        if (playerProfile is not LobbyMemberPlayerProfile lobbyMemberPlayerProfile)
        {
            throw new InvalidOperationException("Failed to construct online multiplayer message name because player is not a lobby member.");
        }

        return $"{nameof(BeatAnalyzedEvent)}-{lobbyMemberPlayerProfile.Name}-{lobbyMemberPlayerProfile.UnityNetcodeClientId}";
    }

    private void OnBeatAnalyzedEventMessage(NamedMessage message)
    {
        if (playerProfile is not LobbyMemberPlayerProfile lobbyMemberPlayerProfile)
        {
            Log.Verbose(() => $"Ignoring BeatAnalyzedEventNetcodeRequestDto from Netcode client {message.SenderNetcodeClientId} because this PlayerMicPitchTracker handles a local player");
            return;
        }

        if (message.SenderNetcodeClientId != lobbyMemberPlayerProfile.UnityNetcodeClientId)
        {
            Log.Verbose(() => $"Ignoring BeatAnalyzedEventNetcodeRequestDto from Netcode client {message.SenderNetcodeClientId} because this PlayerMicPitchTracker handles client {lobbyMemberPlayerProfile.UnityNetcodeClientId} with name '{playerProfile.Name}'");
            return;
        }

        ReadBeatAnalyzedEventFastBufferReader(
            message.MessagePayload,
            out int midiNote,
            out float frequency,
            out int beat,
            out int recordedMidiNote,
            out int roundedRecordedMidiNote);

        FireBeatAnalyzedEventFromRemote(
            message.SenderNetcodeClientId,
            midiNote > 0 ? midiNote : 0,
            frequency > 0 ? frequency : 0,
            beat,
            recordedMidiNote,
            roundedRecordedMidiNote);
    }

    private FastBufferWriter CreateBeatAnalyzedEventFastBufferWriter(BeatAnalyzedEvent beatAnalyzedEvent)
    {
        // The BeatAnalyzedEvent is fired often (multiple times per second) such that a fast (de)serialization is mandatory.
        // This is why a compact representation is created here instead of JSON.
        int size = FastBufferWriter.GetWriteSize<int>() // midiNote
                   + FastBufferWriter.GetWriteSize<float>() // frequency
                   + FastBufferWriter.GetWriteSize<int>() // beat
                   + FastBufferWriter.GetWriteSize<int>() // recordedMidiNote
                   + FastBufferWriter.GetWriteSize<int>(); // roundedRecordedMidiNote
        FastBufferWriter fastBufferWriter = new(size, Allocator.Temp);
        fastBufferWriter.WriteValueSafe(beatAnalyzedEvent.PitchEvent?.MidiNote ?? -1);
        fastBufferWriter.WriteValueSafe(beatAnalyzedEvent.PitchEvent?.Frequency ?? -1);
        fastBufferWriter.WriteValueSafe(beatAnalyzedEvent.Beat);
        fastBufferWriter.WriteValueSafe(beatAnalyzedEvent.RecordedMidiNote);
        fastBufferWriter.WriteValueSafe(beatAnalyzedEvent.RoundedRecordedMidiNote);
        return fastBufferWriter;
    }

    private void ReadBeatAnalyzedEventFastBufferReader(
        FastBufferReader fastBufferReader,
        out int midiNote,
        out float frequency,
        out int beat,
        out int recordedMidiNote,
        out int roundedRecordedMidiNote)
    {
        // The BeatAnalyzedEvent is fired often (multiple times per second) such that a fast (de)serialization is mandatory.
        // This is why a compact representation is read here instead of JSON.
        fastBufferReader.ReadValueSafe(out midiNote);
        fastBufferReader.ReadValueSafe(out frequency);
        fastBufferReader.ReadValueSafe(out beat);
        fastBufferReader.ReadValueSafe(out recordedMidiNote);
        fastBufferReader.ReadValueSafe(out roundedRecordedMidiNote);
    }

    private void FireBeatAnalyzedEventFromRemote(ulong senderNetcodeClientId, int midiNote, float frequency, int beat, int recordedMidiNote, int roundedRecordedMidiNote)
    {
        if (beatsSentToRemote.Contains(beat))
        {
            Log.Verbose(() => $"Should not receive beat from remote because it was sent by this control. beat: {beat}");
            return;
        }

        Log.Verbose(() => $"Fire BeatAnalyzedEvent from Netcode client {senderNetcodeClientId} (beat: {beat}, midiNote: {midiNote})");

        Note noteAtBeat = SongMetaUtils.GetNoteAtBeat(playerControl.GetSortedNotesInVoice(), beat);
        Sentence sentenceAtBeat = SongMetaUtils.GetSentenceAtBeat(playerControl.GetSortedSentencesInVoice(), beat);

        PitchEvent pitchEvent = midiNote > 0
            ? new PitchEvent(midiNote, frequency)
            : null;

        playerMicPitchTracker.FirePitchEvent(pitchEvent, beat, noteAtBeat, sentenceAtBeat);
    }

    private void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
    }
}
