using UnityEngine;
using UnityEngine.UI;

public class AvatarImageReference : MonoBehaviour
{
    public EAvatar avatar;

    public Sprite Sprite
    {
        get
        {
            return GetComponent<Image>().sprite;
        }
    }
}