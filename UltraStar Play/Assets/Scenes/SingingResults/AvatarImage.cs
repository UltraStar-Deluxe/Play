using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AvatarImage : MonoBehaviour
{
    private Image image;

    void OnEnable()
    {
        image = GetComponent<Image>();
    }

    public void SetPlayerProfile(PlayerProfile playerProfile)
    {
        // TODO: Change image
    }
}
