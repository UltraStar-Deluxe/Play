using System.Collections.Generic;

public class VoskResultJson
{
    public List<VoskResultWordJson> result = new();
    public List<VoskResultAlternativeJson> alternatives = new();
    public string text = "";
}
