#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for creating copies of a GameObject arranged in a circular fashion.
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/Tools/Circular Duplicator")]

	public class CircularDuplicator : Duplicator
	{
		[Tooltip("The amount of copies to make of this game object.")]
		[Range(1, 360)]
		public int numberOfCopies = 0;


		public override void ModifyDuplicate(GameObject copy, int counter, float fParameter, out float delay)
		{
			// circular placement
			copy.transform.localRotation = Quaternion.AngleAxis(fParameter * 360.0f, Vector3.up);
			copy.transform.localPosition = Vector3.zero;

			// delay follows a sine motion
			delay = (float) (0.5 * (1 - Mathf.Cos(Mathf.PI * 2 * fParameter)));
		}


		protected override int GetNumberOfCopies()
		{
			return numberOfCopies;
		}

	}

}
