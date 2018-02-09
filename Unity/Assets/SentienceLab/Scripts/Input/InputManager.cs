#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SentienceLab.Input
{
	/// <summary>
	/// Class for managing the mapping of input devices to input handlers.
	/// This class is a simplification and extension at the same time of the Unity Input manager.
	/// 
	/// Make sure the InputManager script is executed AFTER the MoCapManager, 
	/// but before any other script in "Project Settings/Script Execution Order".
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
			UnityInput,
			Keyboard,
			MoCapDevice
		}


		/// <summary>
		/// Structure for associating a handler name to a specific input device.
		/// </summary>
		///
		[System.Serializable]
		public struct InputMap
		{
			public string    inputName;  // name of the input
			public InputType inputType;  // type of the input
			public string    parameters; // parameters
		}


		[Tooltip("Resource with mappings of inputs to keys/devices/etc.")]
		[ContextMenuItem("Copy mapping to Clipboard", "CopyToClipboard")]
		public TextAsset mappingFile;

		[Tooltip("List of mappings of inputs to keys/devices/etc.")]
		[HideInInspector]
		public List<InputMap> mappings;


		public void Start()
		{
			ReadMappings();
			ParseMappings();

			// output list of input handlers
			string logTxt = "";
			foreach (InputHandler h in handlers.Values)
			{
				logTxt += ((logTxt.Length > 0) ? ", " : "") + h.ToString();
			}
			Debug.Log("Loaded input handlers: " + logTxt);
		}


		private void ReadMappings()
		{
			if (mappingFile == null) return;

			// JSON file cannot directly be read because of polymorphism > split manually
			// remove tabs and linefeeds
			string txtConfig = mappingFile.text.Replace("\t", "").Replace("\n", "");
			// cut beginning and end
			txtConfig = Regex.Replace(txtConfig, "^\\s*{\\s*\"Inputs\":\\s*\\[\\s*{", "");
			txtConfig = Regex.Replace(txtConfig, "}\\s*\\]\\s*}\\s*$", "");
			// split
			string[] inputMappings = Regex.Split(txtConfig, "}\\s*,\\s*{");

			foreach (string configTxt in inputMappings)
			{
				try
				{
					string configTxtTrim = "{" + configTxt + "}";
					InputMap map = JsonUtility.FromJson<InputMap>(configTxtTrim);
					InputHandler handler;
					if (!handlers.TryGetValue(map.inputName, out handler))
					{
						handler = new InputHandler(map.inputName);
						handlers[map.inputName] = handler;
					}
					handler.AddMapping(map.inputType, map.parameters);
				}
				catch (System.Exception e)
				{
					Debug.LogWarning("Could not parse Input Mapping Configuration entry " +
						"'" + configTxt + "'" +
						" - Reason: " + e.Message);
				}
			}
		}


		private void ParseMappings()
		{
			// create/update handlers
			foreach (InputMap map in mappings)
			{
				InputHandler handler;
				if (!handlers.TryGetValue(map.inputName, out handler))
				{
					handler = new InputHandler(map.inputName);
					handlers[map.inputName] = handler;
				}
				handler.AddMapping(map.inputType, map.parameters);
			}
		}


		public void Update()
		{
			foreach (InputHandler handler in handlers.Values)
			{
				handler.Process();
			}
		}


		public void OnDestroy()
		{
			handlers.Clear();
		}


#if UNITY_EDITOR

		public void CopyToClipboard()
		{
			string json = "{\n\t\"Inputs\":[";
			bool firstLine = true;
			foreach(InputMap map in mappings)
			{
				if (!firstLine) json += ",";
				json += "\n";
				json += Regex.Replace(JsonUtility.ToJson(map, true), "^\\s*", "\t\t", RegexOptions.Multiline);
				firstLine = false;
			}
			json += "\n\t]\n}";
			EditorGUIUtility.systemCopyBuffer = json;
		}

#endif

		/// <summary>
		/// Retrieves a specific input handler based on the name.
		/// </summary>
		/// <param name="name">the handler name to look for</param>
		/// <returns>the input handler</returns>
		/// 
		public static InputHandler GetInputHandler(string name)
		{
			InputHandler handler;
			if (!handlers.TryGetValue(name, out handler))
			{
				handler = new InputHandler(name);
				handlers[name] = handler;
			}
			return handler;
		}


		private static Dictionary<string, InputHandler> handlers = new Dictionary<string, InputHandler>();
	}
}