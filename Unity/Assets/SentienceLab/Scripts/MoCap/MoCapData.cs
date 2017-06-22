#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for storing a single MoCap data point (e.g., a marker or a bone).
	/// </summary>
	///
	public class MoCapData
	{
		public readonly MoCapDataBuffer buffer; // buffer that owns this data point

		public Vector3    pos;     // position of marker or bone
		public Quaternion rot;     // orientation of bone
		public bool       tracked; // tracking flag
		public float      length;  // length of bone


		/// <summary>
		/// Creates a cloned MoCap data object.
		/// </summary>
		/// 
		public MoCapData(MoCapData clone)
		{
			buffer  = clone.buffer;
			pos     = clone.pos;
			rot     = clone.rot;
			tracked = clone.tracked;
			length  = clone.length;
		}


		/// <summary>
		/// Creates a new MoCap data object.
		/// </summary>
		/// 
		public MoCapData(MoCapDataBuffer buffer)
		{
			this.buffer = buffer;
			pos = new Vector3();
			rot = new Quaternion();
			tracked = false;
			length  = 0;
		}


		/// <summary>
		/// Stores marker data.
		/// </summary>
		/// <param name="marker">the marker to store data of</param>
		/// 
		public void Store(Marker marker)
		{
			pos.Set(marker.px, marker.py, marker.pz);
			tracked = marker.tracked;
		}


		/// <summary>
		/// Stores bone data.
		/// </summary>
		/// <param name="bone">the bone to store data of</param>
		/// 
		public void Store(Bone bone)
		{
			pos.Set(bone.px, bone.py, bone.pz);
			rot.Set(bone.qx, bone.qy, bone.qz, bone.qw);
			tracked = bone.tracked;
			length  = bone.length;
		}
	}

}