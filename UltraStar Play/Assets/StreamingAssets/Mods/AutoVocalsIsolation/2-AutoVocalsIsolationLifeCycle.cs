using UnityEngine;

public class AutoVocalsIsolationLifeCycle : IOnLoadMod, IOnDisableMod, IOnModInstanceBecomesObsolete
{
    public void OnLoadMod()
    {
        Debug.Log($"{nameof(AutoVocalsIsolationLifeCycle)}.OnLoadMod");
    }

    public void OnDisableMod()
    {
        Debug.Log($"{nameof(AutoVocalsIsolationLifeCycle)}.OnDisableMod");
    }

    public void OnModInstanceBecomesObsolete()
    {
        Debug.Log($"{nameof(AutoVocalsIsolationLifeCycle)}.OnModInstanceBecomesObsolete");
    }
}
