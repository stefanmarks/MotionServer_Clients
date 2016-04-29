using UnityEngine;

namespace MoCap
{
	/// <summary>
	/// Component for scaling MoCap data.
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/Modifier/Scale")]

	public class ScaleModifier : MonoBehaviour, IModifier
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
			data.pos *= scaleFactor;
			data.length *= scaleFactor;
		}
	}

}
