using System.Collections.Generic;
using Jint;
using Jint.Native;

public class RuntimeScriptRegistry
{
    public List<HighscoreProvider> HighscoreProviders { get; private set; } = new();

    public void AddHighscoreProvider(JsValue jsHighscoreProvider)
    {
        HighscoreProviders.Add(new HighscoreProvider(jsHighscoreProvider.AsObject()));
    }
}
