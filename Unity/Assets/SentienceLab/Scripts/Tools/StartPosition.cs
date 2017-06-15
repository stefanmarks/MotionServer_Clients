#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;


namespace SentienceLab
{
	/// <summary>
	/// Component that detects the actual display/device configuration
	/// and (de)activates nodes accordingly.
	/// </summary>
	/// 
	public class StartPosition : MonoBehaviour
	{
		[System.Serializable]
		public struct StartConfiguration
		{
			public ConfigurationManager.Configuration configuration;
			public Vector3                            startPosition;
		}

		[Tooltip("List of the starting points for a specific configuration")]
		public StartConfiguration[] StartPositions;


		void Start()
		{
			// default: stay where you are
			Vector3 startPosition = transform.localPosition;

			// check each configuration
			ConfigurationManager.Configuration c = ConfigurationManager.GetConfiguration();
			foreach (StartConfiguration sc in StartPositions)
			{
				if ( sc.configuration == c )
				{
					startPosition = sc.startPosition;
					break;
				}
			}

			transform.localPosition = startPosition;
		}


		void Update()
		{
			// nothing to do here
		}
	}
}