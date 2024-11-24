//--------------------------------------------------------------------------------------------------------------------------------
// Cartoon FX
// (c) 2012-2020 Jean Moreno
//--------------------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;

// GUI Styles and UI methods

namespace CartoonFX
{
	public static class Styles
	{
		//================================================================================================================================
		// GUI Styles
		//================================================================================================================================

		//================================================================================================================================
		// (x) close button
		static GUIStyle _closeCrossButton;
		public static GUIStyle CloseCrossButton
		{
			get
			{
				if(_closeCrossButton == null)
				{
					//Try to load GUISkin according to its GUID
					//Assumes that its .meta file should always stick with it!
					string guiSkinPath = AssetDatabase.GUIDToAssetPath("02d396fa782e5d7438e231ea9f8be23c");
					var gs = AssetDatabase.LoadAssetAtPath<GUISkin>(guiSkinPath);
					if(gs != null)
					{
						_closeCrossButton = System.Array.Find<GUIStyle>(gs.customStyles, x => x.name == "CloseCrossButton");
					}

					//Else fall back to minibutton
					if(_closeCrossButton == null)
						_closeCrossButton = EditorStyles.miniButton;
				}
				return _closeCrossButton;
			}
		}

		//================================================================================================================================
		// Shuriken Toggle with label alignment fix
		static GUIStyle _shurikenToggle;
		public static GUIStyle ShurikenToggle
		{
			get
			{
				if(_shurikenToggle == null)
				{
					_shurikenToggle = new GUIStyle("ShurikenToggle");
					_shurikenToggle.fontSize = 9;
					_shurikenToggle.contentOffset = new Vector2(16, -1);
					if(EditorGUIUtility.isProSkin)
					{
						var textColor = new Color(.8f, .8f, .8f);
						_shurikenToggle.normal.textColor = textColor;
						_shurikenToggle.active.textColor = textColor;
						_shurikenToggle.focused.textColor = textColor;
						_shurikenToggle.hover.textColor = textColor;
						_shurikenToggle.onNormal.textColor = textColor;
						_shurikenToggle.onActive.textColor = textColor;
						_shurikenToggle.onFocused.textColor = textColor;
						_shurikenToggle.onHover.textColor = textColor;
					}
				}
				return _shurikenToggle;
			}
		}

		//================================================================================================================================
		// Bold mini-label (the one from EditorStyles isn't actually "mini")
		static GUIStyle _miniBoldLabel;
		public static GUIStyle MiniBoldLabel
		{
			get
			{
				if(_miniBoldLabel == null)
				{
					_miniBoldLabel = new GUIStyle(EditorStyles.boldLabel);
					_miniBoldLabel.fontSize = 10;
					_miniBoldLabel.margin = new RectOffset(0, 0, 0, 0);
				}
				return _miniBoldLabel;
			}
		}

		//================================================================================================================================
		// Bold mini-foldout
		static GUIStyle _miniBoldFoldout;
		public static GUIStyle MiniBoldFoldout
		{
			get
			{
				if(_miniBoldFoldout == null)
				{
					_miniBoldFoldout = new GUIStyle(EditorStyles.foldout);
					_miniBoldFoldout.fontSize = 10;
					_miniBoldFoldout.fontStyle = FontStyle.Bold;
					_miniBoldFoldout.margin = new RectOffset(0, 0, 0, 0);
				}
				return _miniBoldFoldout;
			}
		}

		//================================================================================================================================
		// Gray right-aligned label for Orderable List (Material Animator)
		static GUIStyle _PropertyTypeLabel;
		public static GUIStyle PropertyTypeLabel
		{
			get
			{
				if(_PropertyTypeLabel == null)
				{
					_PropertyTypeLabel = new GUIStyle(EditorStyles.label);
					_PropertyTypeLabel.alignment = TextAnchor.MiddleRight;
					_PropertyTypeLabel.normal.textColor = Color.gray;
					_PropertyTypeLabel.fontSize = 9;
				}
				return _PropertyTypeLabel;
			}
		}

		// Dark Gray right-aligned label for Orderable List (Material Animator)
		static GUIStyle _PropertyTypeLabelFocused;
		public static GUIStyle PropertyTypeLabelFocused
		{
			get
			{
				if(_PropertyTypeLabelFocused == null)
				{
					_PropertyTypeLabelFocused = new GUIStyle(EditorStyles.label);
					_PropertyTypeLabelFocused.alignment = TextAnchor.MiddleRight;
					_PropertyTypeLabelFocused.normal.textColor = new Color(.2f, .2f, .2f);
					_PropertyTypeLabelFocused.fontSize = 9;
				}
				return _PropertyTypeLabelFocused;
			}
		}

		//================================================================================================================================
		// Rounded Box
		static GUIStyle _roundedBox;
		public static GUIStyle RoundedBox
		{
			get
			{
				if(_roundedBox == null)
				{
					_roundedBox = new GUIStyle(EditorStyles.helpBox);
				}
				return _roundedBox;
			}
		}

		//================================================================================================================================
		// Center White Label ("Editing Spline" label in Scene View)
		static GUIStyle _CenteredWhiteLabel;
		public static GUIStyle CenteredWhiteLabel
		{
			get
			{
				if(_CenteredWhiteLabel == null)
				{
					_CenteredWhiteLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
					_CenteredWhiteLabel.fontSize = 20;
					_CenteredWhiteLabel.normal.textColor = Color.white;
				}
				return _CenteredWhiteLabel;
			}
		}

		//================================================================================================================================
		// Used to draw lines for separators
		static public GUIStyle _LineStyle;
		static public GUIStyle LineStyle
		{
			get
			{
				if(_LineStyle == null)
				{
					_LineStyle = new GUIStyle();
					_LineStyle.normal.background = EditorGUIUtility.whiteTexture;
					_LineStyle.stretchWidth = true;
				}

				return _LineStyle;
			}
		}

		//================================================================================================================================
		// HelpBox with rich text formatting support
		static GUIStyle _HelpBoxRichTextStyle;
		static public GUIStyle HelpBoxRichTextStyle
		{
			get
			{
				if(_HelpBoxRichTextStyle == null)
				{
					_HelpBoxRichTextStyle = new GUIStyle("HelpBox");
					_HelpBoxRichTextStyle.richText = true;
				}
				return _HelpBoxRichTextStyle;
			}
		}

		//================================================================================================================================
		// Material Blue Header
		static public GUIStyle _MaterialHeaderStyle;
		static public GUIStyle MaterialHeaderStyle
		{
			get
			{
				if(_MaterialHeaderStyle == null)
				{
					_MaterialHeaderStyle = new GUIStyle(EditorStyles.label);
					_MaterialHeaderStyle.fontStyle = FontStyle.Bold;
					_MaterialHeaderStyle.fontSize = 11;
					_MaterialHeaderStyle.padding.top = 0;
					_MaterialHeaderStyle.padding.bottom = 0;
					_MaterialHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color32(75, 128, 255, 255) : new Color32(0, 50, 230, 255);
					_MaterialHeaderStyle.stretchWidth = true;
				}

				return _MaterialHeaderStyle;
			}
		}

		//================================================================================================================================
		// Material Header emboss effect
		static public GUIStyle _MaterialHeaderStyleHighlight;
		static public GUIStyle MaterialHeaderStyleHighlight
		{
			get
			{
				if(_MaterialHeaderStyleHighlight == null)
				{
					_MaterialHeaderStyleHighlight = new GUIStyle(MaterialHeaderStyle);
					_MaterialHeaderStyleHighlight.contentOffset = new Vector2(1, 1);
					_MaterialHeaderStyleHighlight.normal.textColor = EditorGUIUtility.isProSkin ? new Color32(255, 255, 255, 16) : new Color32(255, 255, 255, 32);
				}

				return _MaterialHeaderStyleHighlight;
			}
		}

		//================================================================================================================================
		// Filled rectangle

		static private GUIStyle _WhiteRectangleStyle;

		static public void DrawRectangle(Rect position, Color color)
		{
			var col = GUI.color;
			GUI.color *= color;
			DrawRectangle(position);
			GUI.color = col;
		}
		static public void DrawRectangle(Rect position)
		{
			if(_WhiteRectangleStyle == null)
			{
				_WhiteRectangleStyle = new GUIStyle();
				_WhiteRectangleStyle.normal.background = EditorGUIUtility.whiteTexture;
			}

			if(Event.current != null && Event.current.type == EventType.Repaint)
			{
				_WhiteRectangleStyle.Draw(position, false, false, false, false);
			}
		}

		//================================================================================================================================
		// Methods
		//================================================================================================================================

		static public void DrawLine(float height = 2f)
		{
			DrawLine(Color.black, height);
		}
		static public void DrawLine(Color color, float height = 1f)
		{
			Rect position = GUILayoutUtility.GetRect(0f, float.MaxValue, height, height, LineStyle);
			DrawLine(position, color);
		}
		static public void DrawLine(Rect position, Color color)
		{
			if(Event.current.type == EventType.Repaint)
			{
				Color orgColor = GUI.color;
				GUI.color = orgColor * color;
				LineStyle.Draw(position, false, false, false, false);
				GUI.color = orgColor;
			}
		}

		static public void MaterialDrawHeader(GUIContent guiContent)
		{
			var rect = GUILayoutUtility.GetRect(guiContent, MaterialHeaderStyle);
			GUI.Label(rect, guiContent, MaterialHeaderStyleHighlight);
			GUI.Label(rect, guiContent, MaterialHeaderStyle);
		}

		static public void MaterialDrawSeparator()
		{
			GUILayout.Space(4);
			if(EditorGUIUtility.isProSkin)
				DrawLine(new Color(.3f, .3f, .3f, 1f), 1);
			else
				DrawLine(new Color(.6f, .6f, .6f, 1f), 1);
			GUILayout.Space(4);
		}

		static public void MaterialDrawSeparatorDouble()
		{
			GUILayout.Space(6);
			if(EditorGUIUtility.isProSkin)
			{
				DrawLine(new Color(.1f, .1f, .1f, 1f), 1);
				DrawLine(new Color(.4f, .4f, .4f, 1f), 1);
			}
			else
			{
				DrawLine(new Color(.3f, .3f, .3f, 1f), 1);
				DrawLine(new Color(.9f, .9f, .9f, 1f), 1);
			}
			GUILayout.Space(6);
		}

		//built-in console icons, also used in help box
		static Texture2D warnIcon;
		static Texture2D infoIcon;
		static Texture2D errorIcon;

		static public void HelpBoxRichText(Rect position, string message, MessageType msgType)
		{
			Texture2D icon = null;
			switch(msgType)
			{
				case MessageType.Warning: icon = warnIcon ?? (warnIcon = EditorGUIUtility.Load("console.warnicon") as Texture2D); break;
				case MessageType.Info: icon = infoIcon ?? (infoIcon = EditorGUIUtility.Load("console.infoicon") as Texture2D); break;
				case MessageType.Error: icon = errorIcon ?? (errorIcon = EditorGUIUtility.Load("console.erroricon") as Texture2D); break;
			}
			EditorGUI.LabelField(position, GUIContent.none, new GUIContent(message, icon), HelpBoxRichTextStyle);
		}

		static public void HelpBoxRichText(string message, MessageType msgType)
		{
			Texture2D icon = null;
			switch(msgType)
			{
				case MessageType.Warning: icon = warnIcon ?? (warnIcon = EditorGUIUtility.Load("console.warnicon") as Texture2D); break;
				case MessageType.Info: icon = infoIcon ?? (infoIcon = EditorGUIUtility.Load("console.infoicon") as Texture2D); break;
				case MessageType.Error: icon = errorIcon ?? (errorIcon = EditorGUIUtility.Load("console.erroricon") as Texture2D); break;
			}
			EditorGUILayout.LabelField(GUIContent.none, new GUIContent(message, icon), HelpBoxRichTextStyle);
		}
	}
}
