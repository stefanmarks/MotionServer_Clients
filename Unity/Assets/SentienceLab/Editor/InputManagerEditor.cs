#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SentienceLab.Input
{
	/// <summary>
	/// Class for editing the input mappings
	/// </summary>
	/// 

	[CustomEditor(typeof(InputManager))]
	public class InputManagerEditor : Editor
	{
		private ReorderableList list;

		private void OnEnable()
		{
			list = new ReorderableList(serializedObject,
					serializedObject.FindProperty("mappings"),
					true, true, true, true);

			list.drawElementCallback =
				(Rect rect, int index, bool isActive, bool isFocused) => {
					var element = list.serializedProperty.GetArrayElementAtIndex(index);
					rect.y += 2;
					float w = (rect.width - 100) / 2 - 5;
					EditorGUI.PropertyField(
						new Rect(rect.x, rect.y, w, EditorGUIUtility.singleLineHeight),
						element.FindPropertyRelative("inputName"), GUIContent.none);
					EditorGUI.PropertyField(
						new Rect(rect.x + w + 5, rect.y, 100, EditorGUIUtility.singleLineHeight),
						element.FindPropertyRelative("inputType"), GUIContent.none);
					EditorGUI.PropertyField(
						new Rect(rect.xMax - w, rect.y, w, EditorGUIUtility.singleLineHeight),
						element.FindPropertyRelative("parameters"), GUIContent.none);
				};
			list.drawHeaderCallback = (Rect rect) => {
				EditorGUI.LabelField(rect, "Input List");
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
