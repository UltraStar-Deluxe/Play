using Jint;
using Jint.Native;
using Jint.Native.Object;

public class HighscoreProvider
{
    private ObjectInstance jsObject;

    public HighscoreProvider(ObjectInstance jsObject)
    {
        this.jsObject = jsObject;
    }

    public string Name => jsObject.GetProperty("name").Value.AsString();

    public double GetScore()
    {
        return jsObject.Get("getScore").Call(jsObject).AsNumber();
    }
    
    // public double GetNoteCount(SongMeta songMeta)
    // {
    //     JsValue jsSongMeta = JsValue.FromObject(jsObject.Engine, songMeta);
    //     JsValue[] jsParameters = new JsValue[] { jsSongMeta };
    //     return jsObject.Get("getNoteCount").Call(jsObject, jsParameters).AsNumber();
    // }
}
