using UnityEngine;

namespace MoCap
{
	/// <summary>
	/// Component for adding delay to MoCap data.
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/Modifier/Delay")]

	public class DelayModifier : MonoBehaviour, IModifier
	{
		[Tooltip("Delay of the MoCap data in seconds.")]
		[Range(0.0f, 10.0f)]
		public float delay = 0;


		public void Start()
		{
			// empty, but necessary to get the "Enable" button in the inspector
		}


		public void Process(ref MoCapData data)
		{
			// if (!enabled) return;

			// The actual delay happens in the MoCapDataBuffer class
			// by storing the data in a FIFO the length of which
			// is determined by the "delay" value of this component
		}
	}

}
