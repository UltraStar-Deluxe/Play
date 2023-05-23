using UnityEngine.UIElements;

public static class AttributionUtils
{
    public static VisualElement CreateAttributionVisualElement(SongMeta songMeta)
    {
        TextField attributionTextField = new TextField();
        attributionTextField.DisableParseEscapeSequences();
        attributionTextField.multiline = true;
        attributionTextField.isReadOnly = true;
        attributionTextField.AddToClassList("multiline");
        attributionTextField.AddToClassList("noBackground");
        attributionTextField.AddToClassList("smallerFont");
        attributionTextField.AddToClassList("wordWrap");
        attributionTextField.value = SongMetaUtils.GetAttributionText(songMeta);
        return attributionTextField;
    }
}
