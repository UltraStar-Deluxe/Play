using UnityEngine;

/// This is a workaround to pass the value of an enum as parameter
/// to a method callback (e.g. in onClick of a Button).
public class SceneEnumHolder : MonoBehaviour
{
    public EScene scene;
}