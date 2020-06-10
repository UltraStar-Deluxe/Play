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

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private ImageHueHelper imageHueHelper;

    [Inject(optional = true)]
    private MicProfile micProfile;

    public void SetPlayerProfile(PlayerProfile playerProfile)
    {
        AvatarImageReference imageRef = FindObjectsOfType<AvatarImageReference>().Where(it => it.avatar == playerProfile.Avatar).FirstOrDefault();
        if (imageRef != null)
        {
            image.sprite = imageRef.Sprite;
        }
        else
        {
            Debug.LogWarning("Did not find an image for the avatar: " + playerProfile.Avatar);
        }
    }

    public void OnInjectionFinished()
    {
        if (micProfile != null)
        {
            imageHueHelper.SetHueByColor(micProfile.Color);
        }
    }
}
