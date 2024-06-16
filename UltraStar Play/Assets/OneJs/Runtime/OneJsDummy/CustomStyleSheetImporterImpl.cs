#if HAS_ONEJS
#else

using UnityEngine.UIElements;

namespace OneJS.CustomStyleSheets
{
    public class CustomStyleSheetImporterImpl
    {
        public StyleSheet BuildStyleSheet(StyleSheet styleSheet, string styleSheetContent)
        {
            return styleSheet;
        }
    }
}

#endif
