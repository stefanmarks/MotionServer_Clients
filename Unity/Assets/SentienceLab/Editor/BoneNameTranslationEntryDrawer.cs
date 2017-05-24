#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEditor;
using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for rendering a bone translation entry in the editor
	/// </summary>
	/// 

	[CustomPropertyDrawer(typeof(BoneNameTranslationEntry))]

	public class BoneNameTranslationEntryDrawer : PropertyDrawer
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
			float labelWidth2 = 5;
			float space = (position.width - labelWidth1 - labelWidth2) / 5;
			float x = position.x;
			float w = space * 2;
			Rect mocapRect = new Rect(x, position.y, w, position.height);
			x += w; w = labelWidth1;
			Rect arrowRect = new Rect(x, position.y, w, position.height);
			x += w; w = space * 2;
			Rect modelRect = new Rect(x, position.y, w, position.height);
			x += w + labelWidth2; w = space;
			Rect axisRect = new Rect(x, position.y, w, position.height);

			// Draw fields - passs GUIContent.none to each so they are drawn without labels
			EditorGUI.PropertyField(mocapRect, property.FindPropertyRelative("nameMocap"), GUIContent.none);
			EditorGUI.LabelField(arrowRect, " →");
			EditorGUI.PropertyField(modelRect, property.FindPropertyRelative("nameModel"), GUIContent.none);
			EditorGUI.PropertyField(axisRect, property.FindPropertyRelative("axisTransformation"), GUIContent.none);

			// restore indent level
			EditorGUI.indentLevel = oldIndent;

			EditorGUI.EndProperty();
		}
	}
}
