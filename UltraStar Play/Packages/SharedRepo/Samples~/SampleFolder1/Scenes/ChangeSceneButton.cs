using System.Collections;
using System.Collections.Generic;
using ProTrans;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ChangeSceneButton : MonoBehaviour, IPointerClickHandler
{
    public string targetScene;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetScene.IsNullOrEmpty())
        {
            Debug.LogWarning("targetScene not set");
            return;
        }
        SceneManager.LoadScene(targetScene);
    }
}
