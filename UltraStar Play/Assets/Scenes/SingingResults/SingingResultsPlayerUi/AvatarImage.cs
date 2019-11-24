using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AvatarImage : MonoBehaviour
{
    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
    }

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

    public void SetColorOfMicProfile(MicProfile micProfile)
    {
        image.color = micProfile.Color;
    }
}
