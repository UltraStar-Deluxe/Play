using System;
using UnityEngine;

abstract public class AbstractDummySinger : MonoBehaviour
{
    public int playerIndexToSimulate;

    protected PlayerController playerController;

    void Awake()
    {
        if (!Application.isEditor)
        {
            gameObject.SetActive(false);
        }
    }

    public abstract void UpdateSinging(double currentBeat);

    public void SetPlayerController(PlayerController playerController)
    {
        this.playerController = playerController;
        // Disable real microphone input for this player
        playerController.PlayerNoteRecorder.SetMicrophonePitchTrackerEnabled(false);
    }
}