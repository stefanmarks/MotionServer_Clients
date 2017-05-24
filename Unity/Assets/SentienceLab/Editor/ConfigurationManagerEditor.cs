#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Class for editing the object enable/disable mappings
	/// </summary>
	///
	[CustomEditor(typeof(ConfigurationManager))]
	public class ConfigurationManagerEditor : Editor
	{
		private ReorderableList list;

		private void OnEnable()
		{
			list = new ReorderableList(serializedObject,
					serializedObject.FindProperty("objectList"),
					true, true, true, true);

			list.drawElementCallback =
				(Rect rect, int index, bool isActive, bool isFocused) => {
					var element = list.serializedProperty.GetArrayElementAtIndex(index);
					rect.y += 2;
					float w = (rect.width - 100 - 5 - 20) - 5;
					EditorGUI.PropertyField(
						new Rect(rect.x, rect.y, w, EditorGUIUtility.singleLineHeight),
						element.FindPropertyRelative("item"), GUIContent.none);
					EditorGUI.PropertyField(
						new Rect(rect.x + w + 5, rect.y, 100, EditorGUIUtility.singleLineHeight),
						element.FindPropertyRelative("configuration"), GUIContent.none);
					EditorGUI.PropertyField(
						new Rect(rect.xMax - 20, rect.y, 20, EditorGUIUtility.singleLineHeight),
						element.FindPropertyRelative("enable"), GUIContent.none);
				};
			list.drawHeaderCallback = (Rect rect) => {
				EditorGUI.LabelField(rect, "Object List");
			};
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			base.OnInspectorGUI();
			list.DoLayoutList();
			serializedObject.ApplyModifiedProperties();
		}
	}
}
