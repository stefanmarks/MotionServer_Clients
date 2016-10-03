using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace VR.Input
{
	/// <summary>
	/// Class for managing the mapping of input devices to actions.
	/// This class is a simplication and extension at the same time of the Unity Input manager.
	/// 
	/// Make sure the InputManager script is executed AFTER the MoCapManager, but before any other script in "Project Settings/Script Execution Order".
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("Input/Input Manager")]

	public class InputManager : MonoBehaviour
	{
		/// <summary>
		/// Enumeration for the type of input
		/// </summary>
		///
		public enum InputType
		{
			Keyboard,
			Mouse,
			Joystick,
			Touch,
			MoCapDevice
		}


		/// <summary>
		/// Structure for associating an action name to a specific input device.
		/// </summary>
		///
		[System.Serializable]
		public struct ActionMap
		{
			public string    action;     // name of the action
			public InputType inputType;  // type of the input
			public string    inputName;  // name of the input
		}

		[Tooltip("List of mappings of action names to keys/devices/etc.")]
		public List<ActionMap> mappings;


		public void Start()
		{
			ParseMappings();
		}


		private void ParseMappings()
		{
			// create/update handlers
			foreach (ActionMap map in mappings)
			{
				ActionHandler handler;
				if (!handlers.TryGetValue(map.action, out handler))
				{
					handler = new ActionHandler(map.action);
					handlers[map.action] = handler;
				}
				handler.AddMapping(map.inputType, map.inputName);
			}

			// output list of handlers
			string logTxt = "";
			foreach (ActionHandler h in handlers.Values)
			{
				logTxt += ((logTxt.Length > 0) ? ", " : "") + h.ToString();
			}
			Debug.Log("Loaded action handlers: " + logTxt);
		}


		public void Update()
		{
			// nothing to do here
		}


		/// <summary>
		/// Retrieves a specific action handler based on the name.
		/// </summary>
		/// <param name="name">the handler name to look for</param>
		/// <returns>the action handler</returns>
		/// 
		public static ActionHandler GetActionHandler(string name)
		{
			ActionHandler handler;
			if (!handlers.TryGetValue(name, out handler))
			{
				handler = new ActionHandler(name);
				handlers[name] = handler;
			}
			return handler;
		}


		private static Dictionary<string, ActionHandler> handlers = new Dictionary<string, ActionHandler>();
	}

}


#if UNITY_EDITOR

/// <summary>
/// Class for editing the input mappings
/// </summary>
/// 

[CustomEditor(typeof(VR.Input.InputManager))]
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
					element.FindPropertyRelative("action"), GUIContent.none);
				EditorGUI.PropertyField(
					new Rect(rect.x + w + 5, rect.y, 100, EditorGUIUtility.singleLineHeight),
					element.FindPropertyRelative("inputType"), GUIContent.none);
				EditorGUI.PropertyField(
					new Rect(rect.xMax - w, rect.y, w, EditorGUIUtility.singleLineHeight),
					element.FindPropertyRelative("inputName"), GUIContent.none);
			};
		list.drawHeaderCallback = (Rect rect) => {
			EditorGUI.LabelField(rect, "Actions");
		};
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		list.DoLayoutList();
		serializedObject.ApplyModifiedProperties();
	}
}

#endif
