#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using SentienceLab.MoCap;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace SentienceLab
{
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
			HTC_Vive,
			WindowsMixedReality,
			Hololens,
			MagicLeap
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

			if ( MoCapManager.GetInstance().GetDataSourceName().Contains("MotionServer") )
			{
				configuration = Configuration.MoCapRoom;
			}
			else if ( XRDevice.isPresent )
			{
				string model = XRDevice.model.ToLower();
				if (model.Contains("oculus"))
				{
					configuration = Configuration.OculusRift;
				}
				else if (model.Contains("vive"))
				{
					configuration = Configuration.HTC_Vive;
				}
				else if (model.Contains("windows"))
				{
					configuration = Configuration.WindowsMixedReality;
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
}