using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[RequireComponent(typeof(Image))]
public class AvatarImage : MonoBehaviour, INeedInjection, IExcludeFromSceneInjection, IInjectionFinishedListener
{
    [Inject(searchMethod = SearchMethods.GetComponent)]
    private Image image;

    [Inject(optional = true)]
    private MicProfile micProfile;

    [Inject(optional = true)]
    private PlayerProfile playerProfile;
    
    public void OnInjectionFinished()
    {
        if (micProfile != null)
        {
            image.color = micProfile.Color;
        }

        if (playerProfile != null)
        {
            AvatarImageReference imageRef = FindObjectsOfType<AvatarImageReference>()
                .FirstOrDefault(it => it.avatar == playerProfile.Avatar);
            if (imageRef != null)
            {
                image.sprite = imageRef.Sprite;
            }
            else
            {
                Debug.LogWarning("Did not find an image for the avatar: " + playerProfile.Avatar);
            }
        }
    }
}
