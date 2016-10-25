using UnityEngine;
using SentienceLab.MoCap;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

/// <summary>
/// Component that detects the actual display/device configuration
/// and (de)activates nodes accordingly.
/// </summary>
/// 
public class ConfigurationManager : MonoBehaviour
{
	public enum Configuration
	{
		Standalone,
		MoCapRoom,
		OculusRift,
		HTC_Vive
	}


	[System.Serializable]
	public struct ConfigItem
	{
		public Object        item;
		public Configuration configuration;
		public bool          enable;
	}


	[Tooltip("Game objects to enable/disable for specific configurations")]
	[HideInInspector]
	public List<ConfigItem> objectList;


	public void Start()
	{
		DetectConfiguration();
		Debug.Log("Configuration: " + configuration);
		ProcessConfigurations();
	}


	private static void DetectConfiguration()
	{
		if (detectionDone) return;

		// fallback
		configuration = Configuration.Standalone;

		if ( MoCapManager.GetInstance().GetServerName().Contains("MotionServer") )
		{
			configuration = Configuration.MoCapRoom;
		}
		else if ( UnityEngine.VR.VRDevice.isPresent )
		{
			string model = UnityEngine.VR.VRDevice.model.ToLower();
			if (model.Contains("oculus"))
			{
				configuration = Configuration.OculusRift;
			}
			else if (model.Contains("vive"))
			{
				configuration = Configuration.HTC_Vive;
			}
			else
			{
				Debug.LogWarning("Unknown VR model '" + model + "' found.");
			}
		}

		detectionDone = true;
	}


	public static Configuration GetConfiguration()
	{
		DetectConfiguration();
		return configuration;
	}


	private void ProcessConfigurations()
	{
		foreach (ConfigItem ci in objectList)
		{
			if (ci.configuration.Equals(configuration) && (ci.item != null))
			{
				if (ci.item is GameObject)
				{
					((GameObject)ci.item).SetActive(ci.enable);
				}
				else if (ci.item is MonoBehaviour)
				{
					((MonoBehaviour)ci.item).enabled = ci.enable;
				}
			}
		}
	}


	private static Configuration configuration;
	private static bool          detectionDone = false;
}



#if UNITY_EDITOR

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

#endif
