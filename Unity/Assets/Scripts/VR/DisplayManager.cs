using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;


namespace VR
{
	[DisallowMultipleComponent]
	[AddComponentMenu("VR/Display Manager")]

	public class DisplayManager : MonoBehaviour
	{
		[Tooltip("Text asset with the JSON description of the different VR Display profiles")]
		public TextAsset ConfigFile;


		public void Start()
		{
			ParseDisplayProfiles();
		}


		private void ParseDisplayProfiles()
		{
			string[] displayConfigs = ConfigFile.text.Split(new string[] { "\"Display\":" }, System.StringSplitOptions.RemoveEmptyEntries);

			displays = new List<DisplayConfig>();
			string logTxt = "";
			foreach (string configTxt in displayConfigs)
			{
				try
				{
					string configTxtTrim = configTxt.Replace("\n", "").Trim().TrimEnd(',');
					DisplayConfig config = DisplayConfig.FromJson(configTxtTrim);
					if (config != null)
					{
						displays.Add(config);
						logTxt += ((logTxt.Length > 0) ? ", " : "") + config.Name;
					}
				}
				catch (System.Exception e)
				{
					Debug.LogWarning("Could not parse VR Display Device Configuration entry " +
						"'" + configTxt + "'" +
						" - Reason: " + e.Message );
				}
			}
			Debug.Log("Loaded Display Configurations: " + logTxt);
		}


		public DisplayConfig GetConfig(string name)
		{
			DisplayConfig foundConfig = null;

			if ( displays == null )
			{
				ParseDisplayProfiles();
			}

			foreach ( DisplayConfig config in displays )
			{
				if ( name.Equals(config.Name) )
				{
					foundConfig = config;
					break;
				}
			}

			if ( foundConfig == null )
			{
				Debug.LogWarning("Could not find VR Display Configuration '" + name + "'");
			}

			return foundConfig;
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


		private static List<DisplayConfig> displays = null;
	}
}