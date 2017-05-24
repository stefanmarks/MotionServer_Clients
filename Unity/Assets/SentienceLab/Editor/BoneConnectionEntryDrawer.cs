#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEditor;
using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for rendering a bone connection start/end entry in the editor
	/// </summary>
	///

	[CustomPropertyDrawer(typeof(BoneConnectionEntry))]

	public class BoneConnectionEntryDrawer : PropertyDrawer
	{
		// Draw the property inside the given rect
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			// No indentation
			int oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			// Calculate field positions
			float labelWidth1 = 20;
			float space = (position.width - labelWidth1) / 2;
			float x = position.x;
			float w = space;
			Rect name1Rect = new Rect(x, position.y, w, position.height);
			x += w; w = labelWidth1;
			Rect arrowRect = new Rect(x, position.y, w, position.height);
			x += w; w = space;
			Rect name2Rect = new Rect(x, position.y, w, position.height);

			// Draw fields - passs GUIContent.none to each so they are drawn without labels
			EditorGUI.PropertyField(name1Rect, property.FindPropertyRelative("name1"), GUIContent.none);
			EditorGUI.LabelField(arrowRect, " →");
			EditorGUI.PropertyField(name2Rect, property.FindPropertyRelative("name2"), GUIContent.none);

			// restore indent level
			EditorGUI.indentLevel = oldIndent;

			EditorGUI.EndProperty();
		}
	}
}
