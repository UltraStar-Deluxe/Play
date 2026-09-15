using UnityEngine;

public class SingleColumnSingLayoutLifeCycle : IOnLoadMod, IOnDisableMod, IOnModInstanceBecomesObsolete
{
    public void OnLoadMod()
    {
        Debug.Log($"{nameof(SingleColumnSingLayoutLifeCycle)}.OnLoadMod");
    }

    public void OnDisableMod()
    {
        Debug.Log($"{nameof(SingleColumnSingLayoutLifeCycle)}.OnDisableMod");
    }

    public void OnModInstanceBecomesObsolete()
    {
        Debug.Log($"{nameof(SingleColumnSingLayoutLifeCycle)}.OnModInstanceBecomesObsolete");
    }
}
