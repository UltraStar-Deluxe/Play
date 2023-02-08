 using System.Collections.Generic;
 using UnityEngine;
 using UnityEngine.Scripting;
 using UnityEngine.UIElements;

 // https://answers.unity.com/questions/1865976/ui-toolkit-text-best-fit.html
 public class LabelAutoFit : Label
 {
     [Preserve]
     public new class UxmlFactory : UxmlFactory<LabelAutoFit, UxmlTraits> { }
 
     [Preserve]
     public new class UxmlTraits : Label.UxmlTraits
     {
         readonly UxmlIntAttributeDescription minFontSize = new UxmlIntAttributeDescription
         {
             name = "min-font-size",
             defaultValue = 10,
             restriction = new UxmlValueBounds {min = "1"}
         };
 
         readonly UxmlIntAttributeDescription maxFontSize = new UxmlIntAttributeDescription
         {
             name = "max-font-size",
             defaultValue = 200,
             restriction = new UxmlValueBounds {min = "1"}
         };
 
         public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription { get { yield break; } }
 
         public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
         {
             base.Init(ve, bag, cc);
 
             LabelAutoFit instance = ve as LabelAutoFit;
             instance.minFontSize = Mathf.Max(minFontSize.GetValueFromBag(bag, cc), 1);
             instance.maxFontSize = Mathf.Max(maxFontSize.GetValueFromBag(bag, cc), 1);
             instance.RegisterCallback<GeometryChangedEvent>(instance.OnGeometryChanged);
             instance.style.fontSize = 1; // Triggers OnGeometryChanged callback
         }
     }
 
     // Setting a limit of max text font refreshes from a single OnGeometryChanged to avoid repeating cycles in some extreme cases
     private const int MAX_FONT_REFRESHES = 2;
 
     private int m_textRefreshes = 0;
 
     public int minFontSize { get; set; }
     public int maxFontSize { get; set; }
 
     // Call t$$anonymous$$s if the font size does not update by just setting the text
     // Should probably wait till the end of frame to get the real font size, instead of using t$$anonymous$$s method
     public void SetText(string text)
     {
         this.text = text;
         UpdateFontSize();
     }
 
     private void OnGeometryChanged(GeometryChangedEvent evt)
     {
         UpdateFontSize();
     }
 
     private void UpdateFontSize()
     {
         if (m_textRefreshes < MAX_FONT_REFRESHES)
         {
             Vector2 textSize = MeasureTextSize(text, float.MaxValue, MeasureMode.AtMost, float.MaxValue, MeasureMode.AtMost);
             float fontSize = Mathf.Max(style.fontSize.value.value, 1); // Unity can return a font size of 0 w$$anonymous$$ch would break the auto fit // Should probably wait till the end of frame to get the real font size
             float heightDictatedFontSize = Mathf.Abs(contentRect.height);
             float widthDictatedFontSize = Mathf.Abs(contentRect.width / textSize.x) * fontSize;
             float newFontSize = Mathf.FloorToInt(Mathf.Min(heightDictatedFontSize, widthDictatedFontSize));
             newFontSize = Mathf.Clamp(newFontSize, minFontSize, maxFontSize);
             if (Mathf.Abs(newFontSize - fontSize) > 1)
             {
                 m_textRefreshes++;
                 style.fontSize = new StyleLength(new Length(newFontSize));
             }
         }
         else
         {
             m_textRefreshes = 0;
         }
     }
 }
