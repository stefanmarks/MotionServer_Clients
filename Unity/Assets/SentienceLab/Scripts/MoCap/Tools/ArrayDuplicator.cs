#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for creating copies of a GameObject arranged in an array.
	/// </summary>
	/// 

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/Tools/Array Duplicator")]

	public class ArrayDuplicator : Duplicator
	{
		[Tooltip("The amount of rows to form.")]
		[Range(1, 100)]
		public int numberOfColumns = 1;

		[Tooltip("The amount of columns to form.")]
		[Range(1, 100)]
		public int numberOfRows = 1;

		[Tooltip("The size of the matrix array in X/Z direction.")]
		public Vector2 matrixDimension = new Vector2(1, 1);


		public override void ModifyDuplicate(GameObject copy, int counter, float fParameter, out float delay)
		{
			// matrix placement > calculate position x/z within [-0.5...0.5]
			float x = (counter % numberOfColumns) / (float) Mathf.Max(1, numberOfColumns - 1) - 0.5f;
			float z = (counter / numberOfColumns) / (float) Mathf.Max(1, numberOfRows - 1) - 0.5f;

			copy.transform.localPosition = new Vector3(x * matrixDimension.x, 0, z * matrixDimension.y);

			// delay grows from the centre and reaches maximum at axis extremes
			// (so diagonal elements are delayed by maximumDelay * 1.414)
			delay = Mathf.Sqrt(x * x + z * z) * 2;
		}


		protected override int GetNumberOfCopies()
		{
			return numberOfColumns * numberOfRows;
		}
	}

}
