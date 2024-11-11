//--------------------------------------------------------------------------------------------------------------------------------
// Cartoon FX
// (c) 2012-2020 Jean Moreno
//--------------------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

// Custom material inspector for Stylized FX shaders
// - organize UI using comments in the shader code
// - more flexibility than the material property drawers
// version 2 (dec 2017)

namespace CartoonFX
{
	public class MaterialInspector : ShaderGUI
	{
		//Set by PropertyDrawers to defined if the next properties should be visible
		static private Stack<bool> ShowStack = new Stack<bool>();

		static public bool ShowNextProperty { get; private set; }
		static public void PushShowProperty(bool value)
		{
			ShowStack.Push(ShowNextProperty);
			ShowNextProperty &= value;
		}
		static public void PopShowProperty()
		{
			ShowNextProperty = ShowStack.Pop();
		}

		//--------------------------------------------------------------------------------------------------

		const string kGuiCommandPrefix = "//#";
		const string kGC_IfKeyword = "IF_KEYWORD";
		const string kGC_IfProperty = "IF_PROPERTY";
		const string kGC_EndIf = "END_IF";
		const string kGC_HelpBox = "HELP_BOX";
		const string kGC_Label = "LABEL";

		Dictionary<int, List<GUICommand>> guiCommands = new Dictionary<int, List<GUICommand>>();

		bool initialized = false;
		AssetImporter shaderImporter;
		ulong lastTimestamp;
		void Initialize(MaterialEditor editor, bool force)
		{
			if((!initialized || force) && editor != null)
			{
				initialized = true;

				guiCommands.Clear();

				//Find the shader and parse the source to find special comments that will organize the GUI
				//It's hackish, but at least it allows any character to be used (unlike material property drawers/decorators) and can be used along with property drawers

				var materials = new List<Material>();
				foreach(var o in editor.targets)
				{
					var m = o as Material;
					if(m != null)
						materials.Add(m);
				}
				if(materials.Count > 0 && materials[0].shader != null)
				{
					var path = AssetDatabase.GetAssetPath(materials[0].shader);
					//get asset importer
					shaderImporter = AssetImporter.GetAtPath(path);
					if(shaderImporter != null)
					{
						lastTimestamp = shaderImporter.assetTimeStamp;
					}
					//remove 'Assets' and replace with OS path
					path = Application.dataPath + path.Substring(6);
					//convert to cross-platform path
					path = path.Replace('/', Path.DirectorySeparatorChar);
					//open file for reading
					var lines = File.ReadAllLines(path);

					bool insideProperties = false;
					//regex pattern to find properties, as they need to be counted so that
					//special commands can be inserted at the right position when enumerating them
					var regex = new Regex(@"[a-zA-Z0-9_]+\s*\([^\)]*\)");
					int propertyCount = 0;
					bool insideCommentBlock = false;
					foreach(var l in lines)
					{
						var line = l.TrimStart();

						if(insideProperties)
						{
							bool isComment = line.StartsWith("//");

							if(line.Contains("/*"))
								insideCommentBlock = true;
							if(line.Contains("*/"))
								insideCommentBlock = false;

							//finished properties block?
							if(line.StartsWith("}"))
								break;

							//comment
							if(line.StartsWith(kGuiCommandPrefix))
							{
								string command = line.Substring(kGuiCommandPrefix.Length).TrimStart();
								//space
								if(string.IsNullOrEmpty(command))
									AddGUICommand(propertyCount, new GC_Space());
								//separator
								else if(command.StartsWith("---"))
									AddGUICommand(propertyCount, new GC_Separator());
								//separator
								else if(command.StartsWith("==="))
									AddGUICommand(propertyCount, new GC_SeparatorDouble());
								//if keyword
								else if(command.StartsWith(kGC_IfKeyword))
								{
									var expr = command.Substring(command.LastIndexOf(kGC_IfKeyword) + kGC_IfKeyword.Length + 1);
									AddGUICommand(propertyCount, new GC_IfKeyword() { expression = expr, materials = materials.ToArray() });
								}
								//if property
								else if(command.StartsWith(kGC_IfProperty))
								{
									var expr = command.Substring(command.LastIndexOf(kGC_IfProperty) + kGC_IfProperty.Length + 1);
									AddGUICommand(propertyCount, new GC_IfProperty() { expression = expr, materials = materials.ToArray() });
								}
								//end if
								else if(command.StartsWith(kGC_EndIf))
								{
									AddGUICommand(propertyCount, new GC_EndIf());
								}
								//help box
								else if(command.StartsWith(kGC_HelpBox))
								{
									var messageType = MessageType.Error;
									var message = "Invalid format for HELP_BOX:\n" + command;
									var cmd = command.Substring(command.LastIndexOf(kGC_HelpBox) + kGC_HelpBox.Length + 1).Split(new string[] { "::" }, System.StringSplitOptions.RemoveEmptyEntries);
									if(cmd.Length == 1)
									{
										message = cmd[0];
										messageType = MessageType.None;
									}
									else if(cmd.Length == 2)
									{
										try
										{
											var msgType = (MessageType)System.Enum.Parse(typeof(MessageType), cmd[0], true);
											message = cmd[1].Replace("  ", "\n");
											messageType = msgType;
										}
										catch { }
									}
									
									AddGUICommand(propertyCount, new GC_HelpBox()
									{
										message = message,
										messageType = messageType
									});
								}
								//label
								else if(command.StartsWith(kGC_Label))
								{
									var label = command.Substring(command.LastIndexOf(kGC_Label) + kGC_Label.Length + 1);
									AddGUICommand(propertyCount, new GC_Label() { label = label });
								}
								//header: plain text after command
								else
								{
									AddGUICommand(propertyCount, new GC_Header() { label = command });
								}
							}
							else
							//property
							{
								if(regex.IsMatch(line) && !insideCommentBlock && !isComment)
									propertyCount++;
							}
						}

						//start properties block?
						if(line.StartsWith("Properties"))
						{
							insideProperties = true;
						}
					}
				}
			}
		}

		void AddGUICommand(int propertyIndex, GUICommand command)
		{
			if(!guiCommands.ContainsKey(propertyIndex))
				guiCommands.Add(propertyIndex, new List<GUICommand>());

			guiCommands[propertyIndex].Add(command);
		}

		public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
		{
			initialized = false;
			base.AssignNewShaderToMaterial(material, oldShader, newShader);
		}

		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
		{
			//init:
			//- read metadata in properties comment to generate ui layout
			//- force update if timestamp doesn't match last (= file externally updated)
			bool force = (shaderImporter != null && shaderImporter.assetTimeStamp != lastTimestamp);
			Initialize(materialEditor, force);

			var shader = (materialEditor.target as Material).shader;
			materialEditor.SetDefaultGUIWidths();

			//show all properties by default
			ShowNextProperty = true;
			ShowStack.Clear();

			for(int i = 0; i < properties.Length; i++)
			{
				if(guiCommands.ContainsKey(i))
				{
					for(int j = 0; j < guiCommands[i].Count; j++)
					{
						guiCommands[i][j].OnGUI();
					}
				}

				//Use custom properties to enable/disable groups based on keywords
				if(ShowNextProperty)
				{
					if((properties[i].flags & (MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData)) == MaterialProperty.PropFlags.None)
					{
						DisplayProperty(properties[i], materialEditor);
					}
				}
			}

			//make sure to show gui commands that are after properties
			int index = properties.Length;
			if(guiCommands.ContainsKey(index))
			{
				for(int j = 0; j < guiCommands[index].Count; j++)
				{
					guiCommands[index][j].OnGUI();
				}
			}

			//Special fields
			Styles.MaterialDrawSeparatorDouble();
			materialEditor.RenderQueueField();
			materialEditor.EnableInstancingField();
		}

		virtual protected void DisplayProperty(MaterialProperty property, MaterialEditor materialEditor)
		{
			float propertyHeight = materialEditor.GetPropertyHeight(property, property.displayName);
			Rect controlRect = EditorGUILayout.GetControlRect(true, propertyHeight, EditorStyles.layerMaskField, new GUILayoutOption[0]);
			materialEditor.ShaderProperty(controlRect, property, property.displayName);
		}
	}

	// Same as Toggle drawer, but doesn't set any keyword
	// This will avoid adding unnecessary shader keyword to the project
	internal class MaterialToggleNoKeywordDrawer : MaterialPropertyDrawer
	{
		private static bool IsPropertyTypeSuitable(MaterialProperty prop)
		{
			return prop.type == MaterialProperty.PropType.Float || prop.type == MaterialProperty.PropType.Range;
		}

		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			float height;
			if (!MaterialToggleNoKeywordDrawer.IsPropertyTypeSuitable(prop))
			{
				height = 40f;
			}
			else
			{
				height = base.GetPropertyHeight(prop, label, editor);
			}
			return height;
		}

		public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			if (!MaterialToggleNoKeywordDrawer.IsPropertyTypeSuitable(prop))
			{
				EditorGUI.HelpBox(position, "Toggle used on a non-float property: " + prop.name, MessageType.Warning);
			}
			else
			{
				EditorGUI.BeginChangeCheck();
				bool flag = Mathf.Abs(prop.floatValue) > 0.001f;
				EditorGUI.showMixedValue = prop.hasMixedValue;
				flag = EditorGUI.Toggle(position, label, flag);
				EditorGUI.showMixedValue = false;
				if (EditorGUI.EndChangeCheck())
				{
					prop.floatValue = ((!flag) ? 0f : 1f);
				}
			}
		}
	}

	// Same as KeywordEnum drawer, but uses the keyword supplied as is rather than adding a prefix to them
	internal class MaterialKeywordEnumNoPrefixDrawer : MaterialPropertyDrawer
	{
		private readonly GUIContent[] labels;
		private readonly string[] keywords;

		public MaterialKeywordEnumNoPrefixDrawer(string lbl1, string kw1) : this(new[] { lbl1 }, new[] { kw1 }) { }
		public MaterialKeywordEnumNoPrefixDrawer(string lbl1, string kw1, string lbl2, string kw2) : this(new[] { lbl1, lbl2 }, new[] { kw1, kw2 }) { }
		public MaterialKeywordEnumNoPrefixDrawer(string lbl1, string kw1, string lbl2, string kw2, string lbl3, string kw3) : this(new[] { lbl1, lbl2, lbl3 }, new[] { kw1, kw2, kw3 }) { }
		public MaterialKeywordEnumNoPrefixDrawer(string lbl1, string kw1, string lbl2, string kw2, string lbl3, string kw3, string lbl4, string kw4) : this(new[] { lbl1, lbl2, lbl3, lbl4 }, new[] { kw1, kw2, kw3, kw4 }) { }
		public MaterialKeywordEnumNoPrefixDrawer(string lbl1, string kw1, string lbl2, string kw2, string lbl3, string kw3, string lbl4, string kw4, string lbl5, string kw5) : this(new[] { lbl1, lbl2, lbl3, lbl4, lbl5 }, new[] { kw1, kw2, kw3, kw4, kw5 }) { }
		public MaterialKeywordEnumNoPrefixDrawer(string lbl1, string kw1, string lbl2, string kw2, string lbl3, string kw3, string lbl4, string kw4, string lbl5, string kw5, string lbl6, string kw6) : this(new[] { lbl1, lbl2, lbl3, lbl4, lbl5, lbl6 }, new[] { kw1, kw2, kw3, kw4, kw5, kw6 }) { }

		public MaterialKeywordEnumNoPrefixDrawer(string[] labels, string[] keywords)
		{
			this.labels= new GUIContent[keywords.Length];
			this.keywords = new string[keywords.Length];
			for (int i = 0; i < keywords.Length; ++i)
			{
				this.labels[i] = new GUIContent(labels[i]);
				this.keywords[i] = keywords[i];
			}
		}

		static bool IsPropertyTypeSuitable(MaterialProperty prop)
		{
			return prop.type == MaterialProperty.PropType.Float || prop.type == MaterialProperty.PropType.Range;
		}

		void SetKeyword(MaterialProperty prop, int index)
		{
			for (int i = 0; i < keywords.Length; ++i)
			{
				string keyword = GetKeywordName(prop.name, keywords[i]);
				foreach (Material material in prop.targets)
				{
					if (index == i)
						material.EnableKeyword(keyword);
					else
						material.DisableKeyword(keyword);
				}
			}
		}

		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			if (!IsPropertyTypeSuitable(prop))
			{
				return EditorGUIUtility.singleLineHeight * 2.5f;
			}
			return base.GetPropertyHeight(prop, label, editor);
		}

		public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			if (!IsPropertyTypeSuitable(prop))
			{
				EditorGUI.HelpBox(position, "Toggle used on a non-float property: " + prop.name, MessageType.Warning);
				return;
			}

			EditorGUI.BeginChangeCheck();

			EditorGUI.showMixedValue = prop.hasMixedValue;
			var value = (int)prop.floatValue;
			value = EditorGUI.Popup(position, label, value, labels);
			EditorGUI.showMixedValue = false;
			if (EditorGUI.EndChangeCheck())
			{
				prop.floatValue = value;
				SetKeyword(prop, value);
			}
		}

		public override void Apply(MaterialProperty prop)
		{
			base.Apply(prop);
			if (!IsPropertyTypeSuitable(prop))
				return;

			if (prop.hasMixedValue)
				return;

			SetKeyword(prop, (int)prop.floatValue);
		}

		// Final keyword name: property name + "_" + display name. Uppercased,
		// and spaces replaced with underscores.
		private static string GetKeywordName(string propName, string name)
		{
			// Just return the supplied name
			return name;

			// Original code:
			/*
			string n = propName + "_" + name;
			return n.Replace(' ', '_').ToUpperInvariant();
			*/
		}
	}


	//================================================================================================================================================================================================
	// GUI Commands System
	//
	// Workaround to Material Property Drawers limitations:
	// - uses shader comments to organize the GUI, and show/hide properties based on conditions
	// - can use any character (unlike property drawers)
	// - parsed once at material editor initialization

	internal class GUICommand
	{
		public virtual bool Visible() { return true; }
		public virtual void OnGUI() { }
	}

	internal class GC_Separator : GUICommand { public override void OnGUI() { if(MaterialInspector.ShowNextProperty) Styles.MaterialDrawSeparator(); } }
	internal class GC_SeparatorDouble : GUICommand { public override void OnGUI() { if(MaterialInspector.ShowNextProperty) Styles.MaterialDrawSeparatorDouble(); } }
	internal class GC_Space : GUICommand { public override void OnGUI() { if(MaterialInspector.ShowNextProperty) GUILayout.Space(8); } }
	internal class GC_HelpBox : GUICommand
	{
		public string message { get; set; }
		public MessageType messageType { get; set; }

		public override void OnGUI()
		{
			if(MaterialInspector.ShowNextProperty)
				Styles.HelpBoxRichText(message, messageType);
		}
	}
	internal class GC_Header : GUICommand
	{
		public string label { get; set; }
		GUIContent guiContent;

		public override void OnGUI()
		{
			if(guiContent == null)
				guiContent = new GUIContent(label);

			if(MaterialInspector.ShowNextProperty)
				Styles.MaterialDrawHeader(guiContent);
		}
	}
	internal class GC_Label : GUICommand
	{
		public string label { get; set; }
		GUIContent guiContent;

		public override void OnGUI()
		{
			if(guiContent == null)
				guiContent = new GUIContent(label);

			if(MaterialInspector.ShowNextProperty)
				GUILayout.Label(guiContent);
		}
	}
	internal class GC_IfKeyword : GUICommand
	{
		public string expression { get; set; }
		public Material[] materials { get; set; }
		public override void OnGUI()
		{
			bool show = ExpressionParser.EvaluateExpression(expression, (string s) =>
			{
				foreach(var m in materials)
				{
					if(m.IsKeywordEnabled(s))
						return true;
				}
				return false;
			});
			MaterialInspector.PushShowProperty(show);
		}
	}
	internal class GC_EndIf : GUICommand { public override void OnGUI() { MaterialInspector.PopShowProperty(); } }

	internal class GC_IfProperty : GUICommand
	{
		string _expression;
		public string expression
		{
			get { return _expression; }
			set { _expression = value.Replace("!=", "<>"); }
		}
		public Material[] materials { get; set; }

		public override void OnGUI()
		{
			bool show = ExpressionParser.EvaluateExpression(expression, EvaluatePropertyExpression);
			MaterialInspector.PushShowProperty(show);
		}

		bool EvaluatePropertyExpression(string expr)
		{
			//expression is expected to be in the form of: property operator value
			var reader = new StringReader(expr);
			string property = "";
			string op = "";
			float value = 0f;

			int overflow = 0;
			while(true)
			{
				char c = (char)reader.Read();

				//operator
				if(c == '=' || c == '>' || c == '<' || c == '!')
				{
					op += c;
					//second operator character, if any
					char c2 = (char)reader.Peek();
					if(c2 == '=' || c2 == '>')
					{
						reader.Read();
						op += c2;
					}

					//end of string is the value
					var end = reader.ReadToEnd();
					if(!float.TryParse(end, out value))
					{
						Debug.LogError("Couldn't parse float from property expression:\n" + end);
						return false;
					}

					break;
				}

				//property name
				property += c;

				overflow++;
				if(overflow >= 9999)
				{
					Debug.LogError("Expression parsing overflow!\n");
					return false;
				}
			}

			//evaluate property
			bool conditionMet = false;
			foreach(var m in materials)
			{
				float propValue = 0f;
				if(property.Contains(".x") || property.Contains(".y") || property.Contains(".z") || property.Contains(".w"))
				{
					string[] split = property.Split('.');
					string component = split[1];
					switch(component)
					{
						case "x": propValue = m.GetVector(split[0]).x; break;
						case "y": propValue = m.GetVector(split[0]).y; break;
						case "z": propValue = m.GetVector(split[0]).z; break;
						case "w": propValue = m.GetVector(split[0]).w; break;
						default: Debug.LogError("Invalid component for vector property: '" + property + "'"); break;
					}
				}
				else
					propValue = m.GetFloat(property);

				switch(op)
				{
					case ">=": conditionMet = propValue >= value; break;
					case "<=": conditionMet = propValue <= value; break;
					case ">": conditionMet = propValue > value; break;
					case "<": conditionMet = propValue < value; break;
					case "<>": conditionMet = propValue != value; break;	//not equal, "!=" is replaced by "<>" to prevent bug with leading ! ("not" operator)
					case "==": conditionMet = propValue == value; break;
					default:
						Debug.LogError("Invalid property expression:\n" + expr);
						break;
				}
				if(conditionMet)
					return true;
			}

			return false;
		}
	}
}