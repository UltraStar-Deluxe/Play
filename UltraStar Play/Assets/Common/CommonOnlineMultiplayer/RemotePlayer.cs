using UnityEngine;

/**
 * An instance of this component is created for every client that joins a hosted game.
 * Therefor, this component is part of the prefab that is created by Unity NetworkManager when a new client connects.
 */
public class RemotePlayer : MonoBehaviour
{
    private void Start()
    {
        // DontDestroyOnLoad object to persist the object across (custom implementation of) scene changes.
        DontDestroyOnLoad(this);
    }
}
