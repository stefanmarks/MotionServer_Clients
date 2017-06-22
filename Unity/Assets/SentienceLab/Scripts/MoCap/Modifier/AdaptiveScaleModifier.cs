#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Component for scaling MoCap data based on the distance to another object.
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/Modifier/Adaptive Scale")]

	public class AdaptiveScaleModifier : MonoBehaviour, IMoCapDataModifier
	{
		[Tooltip("Transform to measure the relative distance to")]
		public Transform centreObject;

		[Tooltip("Scale factor curve based on distance of MoCap object to the centre object.")]
		public AnimationCurve curve = AnimationCurve.Linear(0, 1, 2, 1);

		[Tooltip("Set flag to ignore the Y axis in the distance calculation and the scaling operation")]
		public bool ignoreY_Axis = true;


		public void Start()
		{
			curve.preWrapMode  = WrapMode.Clamp;
			curve.postWrapMode = WrapMode.Clamp;
		}


		public void Process(ref MoCapData data)
		{
			if (!enabled) return;

			// build relative distance to centre object
			Vector3 offset = centreObject.localPosition;
			data.pos -= offset;

			// calculate distance (possibly ignoring Y)
			Vector3 distVec = data.pos;
			if (ignoreY_Axis) { distVec.y = 0; }
			float scaleFactor = curve.Evaluate(distVec.magnitude);

			// scale object position
			data.pos.x *= scaleFactor;
			data.pos.z *= scaleFactor;
			if (!ignoreY_Axis) { data.pos.y *= scaleFactor; }

			data.length *= scaleFactor;

			// turn back to absolute coordinate
			data.pos += offset;
		}


		public int GetRequiredBufferSize()
		{
			return 1;
		}
	}
}
