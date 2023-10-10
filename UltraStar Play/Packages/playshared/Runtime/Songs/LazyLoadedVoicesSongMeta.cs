using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LazyLoadedVoicesSongMeta : SongMeta
{
    private enum ELoadVoicesPhase
    {
        Pending,
        Started,
        FinishedSuccessfully,
        Failed,
    }

    public virtual Action OnLoadVoices { get; set; }

    public bool HasFailedToLoadVoices => loadVoicesPhase is ELoadVoicesPhase.Failed;
    private ELoadVoicesPhase loadVoicesPhase;

    public override string GetVoiceDisplayName(EVoiceId voiceId)
    {
        if (voiceIdToDisplayName.IsNullOrEmpty())
        {
            LoadVoicesIfNotDoneYet();
        }
        return base.GetVoiceDisplayName(voiceId);
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

        if (loadVoicesPhase is ELoadVoicesPhase.Pending)
        {
            // No need to load the voices anymore.
            loadVoicesPhase = ELoadVoicesPhase.FinishedSuccessfully;
        }
    }

    public virtual void LoadVoicesIfNotDoneYet()
    {
        if (loadVoicesPhase is not ELoadVoicesPhase.Pending)
        {
            return;
        }

        try
        {
            loadVoicesPhase = ELoadVoicesPhase.Started;
            if (OnLoadVoices == null)
            {
                LoadDefaultVoices();
            }
            else
            {
                OnLoadVoices();
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to lazy load voices of '{SongMetaUtils.GetArtistDashTitle(this)}': {ex.Message}");
            loadVoicesPhase = ELoadVoicesPhase.Failed;
            return;
        }

        loadVoicesPhase = ELoadVoicesPhase.FinishedSuccessfully;
    }

    private void LoadDefaultVoices()
    {
        // Create empty voice by default
        AddVoice(new Voice(EVoiceId.P1));
    }
}
