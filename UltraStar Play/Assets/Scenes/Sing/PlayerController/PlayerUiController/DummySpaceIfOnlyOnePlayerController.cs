using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The vertical layout group of the PlayerUiArea should only take half the vertical space if there is one element.
// To ensure this (as a workaround) there is an empty dummy object.
// The dummy removes itself if there is more than one element in the PlayerUiArea.
public class DummySpaceIfOnlyOnePlayerController : MonoBehaviour
{
    void Update()
    {
        if (SingSceneController.Instance.PlayerControllers.Count > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            transform.SetAsLastSibling();
        }
    }
}
