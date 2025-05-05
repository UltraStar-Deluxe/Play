using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public abstract class LazyLoadedVoicesSongMeta : SongMeta
{
    public enum ELoadVoicesPhase
    {
        Pending,
        Started,
        FinishedSuccessfully,
        Failed,
    }

    [JsonIgnore]
    public virtual Action DoLoadVoices { get; set; }

    [JsonIgnore]
    public ELoadVoicesPhase LoadVoicesPhase { get; private set; }

    public string FailedToLoadVoicesExceptionMessage => failedToLoadVoicesExceptionMessage;
    private string failedToLoadVoicesExceptionMessage;

    public override string GetVoiceDisplayName(EVoiceId voiceId)
    {
        LoadVoicesIfNotDoneYet();
        return base.GetVoiceDisplayName(voiceId);
    }

    public override int VoiceCount
    {
        get
        {
            LoadVoicesIfNotDoneYet();

            return base.VoiceCount;
        }
    }

    public override IReadOnlyCollection<Voice> Voices
    {
        get
        {
            LoadVoicesIfNotDoneYet();

            return base.Voices;
        }
    }

    public override bool TryGetVoice(EVoiceId voiceId, out Voice voice)
    {
        LoadVoicesIfNotDoneYet();

        return base.TryGetVoice(voiceId, out voice);
    }

    public override void AddVoice(Voice voice)
    {
        base.AddVoice(voice);

        if (LoadVoicesPhase is ELoadVoicesPhase.Pending)
        {
            // No need to load the voices anymore.
            LoadVoicesPhase = ELoadVoicesPhase.FinishedSuccessfully;
        }
    }

    public virtual void LoadVoicesIfNotDoneYet()
    {
        if (LoadVoicesPhase is not ELoadVoicesPhase.Pending)
        {
            return;
        }

        try
        {
            LoadVoicesPhase = ELoadVoicesPhase.Started;
            if (DoLoadVoices == null)
            {
                LoadDefaultVoices();
            }
            else
            {
                DoLoadVoices();
            }
        }
        catch (Exception ex)
        {
            LoadVoicesPhase = ELoadVoicesPhase.Failed;
            failedToLoadVoicesExceptionMessage = ex.Message;
            Debug.LogException(ex);
            Debug.LogError($"Failed to load voices of '{this.GetArtistDashTitle()}': {ex.Message}");
            return;
        }

        LoadVoicesPhase = ELoadVoicesPhase.FinishedSuccessfully;
    }

    private void LoadDefaultVoices()
    {
        // Create empty voice by default
        AddVoice(new Voice(EVoiceId.P1));
    }
}
