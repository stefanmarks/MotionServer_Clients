#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Component for scaling MoCap data selectively by bone/marker name.
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/Modifier/Selective Scale")]

	public class SelectiveScaleModifier : MonoBehaviour, IMoCapDataModifier
	{
		[Tooltip("Homogeneous scale factor.")]
		public float scaleFactor = 1.0f;

		[Tooltip("Prefix for any bone or marker name.")]
		public string namePrefix = "";

		[Tooltip("Names of bones or markers to selectively scale.")]
		public string[] names = { };


		public void Start()
		{
			// empty, but necessary to get the "Enable" button in the inspector
		}


		public void Process(ref MoCapData data)
		{
			if (!enabled) return;

			foreach (string name in names)
			{
				if (data.buffer.GetName().Equals(namePrefix + name))
				{
					data.pos    *= scaleFactor;
					data.length *= scaleFactor;
					break;
				}
			}
		}


		public int GetRequiredBufferSize()
		{
			return 1;
		}
	}
}
