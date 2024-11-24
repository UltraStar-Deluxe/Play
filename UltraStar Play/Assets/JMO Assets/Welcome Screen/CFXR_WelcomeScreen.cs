using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CartoonFX
{
    [InitializeOnLoad]
    public class CFXR_WelcomeScreen : EditorWindow
    {
        static CFXR_WelcomeScreen()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool("CFXR_WelcomeScreen_Shown", false))
                {
                    return;
                }
            SessionState.SetBool("CFXR_WelcomeScreen_Shown", true);

                var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath("bfd03f272fe010b4ba558a3bc456ffeb"));
                if (importer != null && importer.userData == "dontshow")
                {
                    return;
                }

                Open();
            };
        }

        [MenuItem("Tools/Cartoon FX Remaster FREE - Welcome Screen")]
        static void Open()
        {
            var window = GetWindow<CFXR_WelcomeScreen>(true, "Cartoon FX Remaster FREE", true);
            window.minSize = new Vector2(516, 370);
            window.maxSize = new Vector2(516, 370);
        }

        void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.height = new StyleLength(new Length(100, LengthUnit.Percent));

            // UXML
            var uxmlDocument = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath("bfd03f272fe010b4ba558a3bc456ffeb"));
            root.Add(uxmlDocument.Instantiate());
            // USS
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath("f8b971f10a610844f968f582415df874"));
            root.styleSheets.Add(styleSheet);

            // Background image
            root.style.backgroundImage = new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath("fed1b64fd853f994c8d504720a0a6d44")));
            root.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;

            // Logo image
            var titleImage = root.Q<Image>("img_title");
            titleImage.image = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath("a665b2e53088caa4c89dd09f9c889f62"));

            // Buttons
            root.Q<Label>("btn_cfxr1").AddManipulator(new Clickable(evt => { Application.OpenURL("https://assetstore.unity.com/packages/slug/4010"); }));
            root.Q<Label>("btn_cfxr2").AddManipulator(new Clickable(evt => { Application.OpenURL("https://assetstore.unity.com/packages/slug/4274"); }));
            root.Q<Label>("btn_cfxr3").AddManipulator(new Clickable(evt => { Application.OpenURL("https://assetstore.unity.com/packages/slug/10172"); }));
            root.Q<Label>("btn_cfxr4").AddManipulator(new Clickable(evt => { Application.OpenURL("https://assetstore.unity.com/packages/slug/23634"); }));
            root.Q<Label>("btn_cfxrbundle").AddManipulator(new Clickable(evt => { Application.OpenURL("https://assetstore.unity.com/packages/slug/232385"); }));

            root.Q<Button>("close_dontshow").RegisterCallback<ClickEvent>(evt =>
            {
                this.Close();
                var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath("bfd03f272fe010b4ba558a3bc456ffeb"));
                importer.userData = "dontshow";
                importer.SaveAndReimport();
            });
            root.Q<Button>("close").RegisterCallback<ClickEvent>(evt => { this.Close(); });
        }
    }
}
