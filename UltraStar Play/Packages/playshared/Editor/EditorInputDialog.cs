using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class EditorInputDialog : EditorWindow
{
    public static string Show(string pTitle, string pDescription, string pText, string pOkButton = "Ok", string pCancelButton = "Cancel")
    {
        string result = null;
        EditorInputDialog window = CreateInstance<EditorInputDialog>();
        window.titleContent = new GUIContent(pTitle);
        window.rootVisualElement.style.height = new Length(100, LengthUnit.Percent);
        window.rootVisualElement.style.justifyContent = new StyleEnum<Justify>(Justify.SpaceAround);

        Label label = new Label(pDescription);
        window.rootVisualElement.Add(label);

        TextField inputText = new TextField();
        inputText.value = pText;
        window.rootVisualElement.Add(inputText);

        Button okButton = new Button(() => { result = inputText.value; window.Close(); }) { text = pOkButton };
        okButton.style.flexGrow = 1;
        Button cancelButton = new Button(() => { result = null; window.Close(); }) { text = pCancelButton };
        cancelButton.style.flexGrow = 1;

        VisualElement buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
        buttonContainer.style.justifyContent = new StyleEnum<Justify>(Justify.SpaceAround);
        buttonContainer.Add(okButton);
        buttonContainer.Add(cancelButton);
        window.rootVisualElement.Add(buttonContainer);

        window.rootVisualElement.RegisterCallback<KeyUpEvent>(e =>
        {
            if (e.keyCode == KeyCode.Escape)
            {
                result = null;
                window.Close();
            }

            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                result = inputText.value;
                window.Close();
            }
        });

        // Move window to a new position. Make sure we're inside visible window
        Vector2 mousePos = GUIUtility.GUIToScreenPoint(new Vector2(Screen.width / 2f, Screen.height / 2f));
        Vector2 maxPos = GUIUtility.GUIToScreenPoint(new Vector2(Screen.width, Screen.height));
        mousePos.x += 32;
        if (mousePos.x + window.position.width > maxPos.x) mousePos.x -= window.position.width + 64; // Display on left side of mouse
        if (mousePos.y + window.position.height > maxPos.y) mousePos.y = maxPos.y - window.position.height;

        window.position = new Rect(mousePos.x, mousePos.y, window.position.width, window.position.height);

        window.rootVisualElement.schedule.Execute(() =>
        {
            inputText.Focus();
        }).ExecuteLater(10);

        window.ShowModal();
        return result;
    }
}
