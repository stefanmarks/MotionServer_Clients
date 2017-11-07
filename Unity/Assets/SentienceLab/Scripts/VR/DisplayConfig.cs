#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab
{
	[System.Serializable]
	public enum DisplayType
	{
		Screen,
		HMD
	}


	[System.Serializable]
	public class DisplayConfig
	{
		public DisplayType Type = DisplayType.Screen;
		public string      Name = "";


		public static DisplayConfig FromJson(string jsonTxt)
		{
			// read base config first to get type
			DisplayConfig config = JsonUtility.FromJson<DisplayConfig>(jsonTxt);
			// now create specific type
			switch (config.Type)
			{
				case DisplayType.Screen: config = JsonUtility.FromJson<ScreenConfig>(jsonTxt); break;
				case DisplayType.HMD:    config = JsonUtility.FromJson<HMD_Config>(jsonTxt); break;
				default: break; // just return base type
			}
			return config;
		}
	}


	[System.Serializable]
	public class ScreenConfig : DisplayConfig
	{
		public float FieldOfView = 90;

		public ScreenConfig()
		{
			Type = DisplayType.Screen;
		}
	}


	[System.Serializable]
	public class HMD_Config : DisplayConfig
	{
		public float   IPD         = 0.06f;
		public float   FieldOfView = 90;
		public float   xOffset     = 0;

		public float[] LensDistortionParameters          = { 1, 0, 0, 0 };
		public float[] ChromaticAberrationParametersRed  = { 1, 0 };
		public float[] ChromaticAberrationParametersBlue = { 1, 0 };

		public float   ScaleIn  = 1.0f;
		public float   ScaleOut = 1.0f;

		public HMD_Config()
		{
			Type = DisplayType.HMD;
		}
	}

}
