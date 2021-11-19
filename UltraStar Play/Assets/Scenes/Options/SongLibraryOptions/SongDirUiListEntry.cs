using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class SongDirUiListEntry : MonoBehaviour
{
    public InputField inputField;
    public Button deleteButton;

    public int songDirIndexInList = -1;

    public void SetSongDir(string songDir, int songDirIndexInList)
    {
        inputField.text = songDir;
        this.songDirIndexInList = songDirIndexInList;
    }

}