#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Component for mirroring MoCap data along a principal axis.
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/Modifier/Mirror")]

	public class MirrorModifier : MonoBehaviour, IMoCapDataModifier
	{
		public enum Axis
		{
			X_Axis, Y_Axis, Z_Axis
		}

		[Tooltip("Axis to mirror data along.")]
		public Axis axis = Axis.Z_Axis;


		public void Start()
		{
			// empty, but necessary to get the "Enable" button in the inspector
		}


		public void Process(ref MoCapData data)
		{
			if (!enabled) return;

			switch (axis)
			{
				case Axis.X_Axis:
					data.pos.x = -data.pos.x;
					data.rot.x = -data.rot.x;
					data.rot.w = -data.rot.w;
					break;

				case Axis.Y_Axis:
					data.pos.y = -data.pos.y;
					// TODO: not fully functional with rotation. Why?
					//data.rot.y = -data.rot.y;
					//data.rot.w = -data.rot.w;
					break;

				case Axis.Z_Axis:
					data.pos.z = -data.pos.z;
					data.rot.z = -data.rot.z;
					data.rot.w = -data.rot.w;
					break;
			}
		}

		public int GetRequiredBufferSize()
		{
			return 1;
		}
	}
}
