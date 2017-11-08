#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;

namespace SentienceLab
{
	/// <summary>
	/// Class for managing VR displays and their render and camera settings.
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("VR/Display Manager")]

	public class DisplayManager : MonoBehaviour
	{
		[Tooltip("Text asset with the JSON description of the different VR Display profiles")]
		public TextAsset ConfigFile;


		public void Start()
		{
			ParseDisplayProfiles();

			string dbgOutput = "Connected displays: " + Display.displays.Length + "\n";
			int displayIdx = 1;
			foreach (Display d in Display.displays)
			{
				dbgOutput += "- Display " + displayIdx + ": " + d.systemWidth + "x" + d.systemHeight + "\n";
				displayIdx++;
			}
			Debug.Log(dbgOutput);
		}


		private void ParseDisplayProfiles()
		{
			// JSON file cannot directly be read because of polymorphism > split manually
			// remove tabs and linefeeds
			string txtConfig = ConfigFile.text.Replace("\t", "").Replace("\n", "");
			// cut beginning and end
			txtConfig = Regex.Replace(txtConfig, "^\\s*{\\s*\"Displays\":\\[\\s*{", "");
			txtConfig = Regex.Replace(txtConfig, "}\\s*\\]\\s*}\\s*$", "");
			// split
			string[] displayConfigs = Regex.Split(txtConfig, "}\\s*,\\s*{");

			displays = new Dictionary<string, DisplayConfig>();
			string logTxt = "";
			foreach (string configTxt in displayConfigs)
			{
				try
				{
					string configTxtTrim = "{" + configTxt + "}";
					DisplayConfig config = DisplayConfig.FromJson(configTxtTrim);
					if (config != null)
					{
						displays.Add(SimplifyConfigName(config.Name), config);
						logTxt += ((logTxt.Length > 0) ? ", " : "") + config.Name;
					}
				}
				catch (System.Exception e)
				{
					Debug.LogWarning("Could not parse VR Display Device Configuration entry " +
						"'" + configTxt + "'" +
						" - Reason: " + e.Message);
				}
			}
			Debug.Log("Loaded Display Configurations: " + logTxt);
		}


		private string SimplifyConfigName(string name)
		{
			// remove spaces and convert completely to lowercase 
			return name.Replace(" ", "").ToLower();
		}


		/// <summary>
		/// Retrieves a specific display configuration based on the name.
		/// </summary>
		/// <param name="name">the configuration name to look for</param>
		/// <returns>the configuration or <c>null</c> if the configuration doesn't exist</returns>
		/// 
		public DisplayConfig GetConfig(string name)
		{
			if ( displays == null )
			{
				ParseDisplayProfiles();
			}

			DisplayConfig config = displays[SimplifyConfigName(name)];
			if ( config == null )
			{
				Debug.LogWarning("Could not find VR Display Configuration '" + name + "'");
			}

			return config;
		}


		/// <summary>
		/// Searches for the DeviceManager instance in the scene and returns it
		/// or quits if it is not defined.
		/// </summary>
		/// <returns>the DeviceManager instance</returns>
		/// 
		public static DisplayManager GetInstance()
		{
			// try to find the client instance 
			DisplayManager manager = FindObjectOfType<DisplayManager>();
			Assert.IsNotNull(manager, "No VR.DisplayManager component defined in the scene.");
			return manager;
		}


		private static Dictionary<string, DisplayConfig> displays = null;
	}

}
