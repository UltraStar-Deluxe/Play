using UnityEngine;
using UnityEngine.UI;

public class FocusOnStart : MonoBehaviour
{
    private void Start() {
	    GetComponentInChildren<Selectable>().Select();
    }
}
