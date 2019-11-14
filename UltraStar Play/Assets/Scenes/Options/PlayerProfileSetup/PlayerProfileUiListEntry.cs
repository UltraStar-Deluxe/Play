using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class PlayerProfileUiListEntry : MonoBehaviour
{
    public InputField nameField;
    public AvatarSlider avatarField;
    public DifficultySlider difficultyField;
    public Toggle enabledToggle;
    public Button deleteButton;

    private PlayerProfile playerProfile;

    public void SetPlayerProfile(PlayerProfile playerProfile)
    {
        if (this.playerProfile != null)
        {
            throw new UnityException("PlayerProfile was set already.");
        }

        this.playerProfile = playerProfile;
        nameField.text = playerProfile.Name;
        avatarField.Selection.Value = playerProfile.Avatar;
        difficultyField.Selection.Value = playerProfile.Difficulty;
        enabledToggle.isOn = playerProfile.IsEnabled;

        enabledToggle.OnValueChangedAsObservable().Subscribe(newValue => playerProfile.IsEnabled = newValue);
        nameField.OnValueChangedAsObservable().Subscribe(newValue => playerProfile.Name = newValue);
        avatarField.Selection.Subscribe(newValue => playerProfile.Avatar = newValue);
        difficultyField.Selection.Subscribe(newValue => playerProfile.Difficulty = newValue);
    }
}
