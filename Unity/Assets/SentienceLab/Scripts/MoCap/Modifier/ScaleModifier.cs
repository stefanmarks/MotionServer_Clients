#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Component for scaling MoCap data.
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/Modifier/Scale")]

	public class ScaleModifier : MonoBehaviour, IMoCapDataModifier
	{
		[Tooltip("Homogeneous scale factor.")]
		public float scaleFactor = 1.0f;


		public void Start()
		{
			// empty, but necessary to get the "Enable" button in the inspector
		}


		public void Process(ref MoCapData data)
		{
			if (!enabled) return;
			data.pos    *= scaleFactor;
			data.length *= scaleFactor;
		}


		public int GetRequiredBufferSize()
		{
			return 1;
		}
	}
}
