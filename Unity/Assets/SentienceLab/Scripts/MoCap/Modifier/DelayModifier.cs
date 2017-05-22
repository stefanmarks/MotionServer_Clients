#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Component for adding delay to MoCap data.
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/Modifier/Delay")]

	public class DelayModifier : MonoBehaviour, IMoCapDataModifier
	{
		[Tooltip("Delay of the MoCap data in seconds.")]
		[Range(0.0f, 10.0f)]
		public float delay = 0;


		public void Start()
		{
			framerate = MoCapManager.GetInstance().GetFramerate();
		}


		public void Process(ref MoCapData data)
		{
			// replace by object further down in the pipeline depending on the delay
			data = data.buffer.GetElement(GetRequiredBufferSize() - 1);
		}


		public int GetRequiredBufferSize()
		{
			return Mathf.Max(1, 1 + (int)(delay * framerate));
		}


		private float framerate;
	}

}
