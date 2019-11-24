using UnityEngine;

abstract public class AbstractDummySinger : MonoBehaviour
{
    public int playerIndexToSimulate;

    private SingSceneController singSceneController;

    private PlayerController PlayerController
    {
        get
        {
            if (singSceneController.PlayerControllers.Count > playerIndexToSimulate)
            {
                return singSceneController.PlayerControllers[playerIndexToSimulate];
            }
            else
            {
                return null;
            }
        }
    }

    void Awake()
    {
        if (!Application.isEditor)
        {
            gameObject.SetActive(false);
        }
    }

    void Start()
    {
        singSceneController = SingSceneController.Instance;
    }

    void Update()
    {
        if (PlayerController == null)
        {
            return;
        }

        // Disable normal pitch detection of the PlayerController
        PlayerController.PlayerNoteRecorder.SetMicrophonePitchTrackerEnabled(false);

        // Simulate singing
        double currentBeat = singSceneController.CurrentBeat;
        UpdateSinging(PlayerController, currentBeat);
    }

    protected abstract void UpdateSinging(PlayerController playerController, double currentBeat);
}