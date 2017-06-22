#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

/// <summary>
/// Untility functions for mathematical operations.
/// </summary>
/// 
namespace SentienceLab
{
	public static class MathUtil
	{
		/// <summary>
		/// Returns the rotation part of a 4x4 matrix.
		/// </summary>
		/// <param name="matrix">the matrix to extract the rotation from</param>
		/// <returns>the rotation part of the matrix as a quaternion</returns>
		/// 
		public static Quaternion GetRotation(Matrix4x4 matrix)
		{
			quaternion.w  = Mathf.Sqrt(Mathf.Max(0, 1 + matrix.m00 + matrix.m11 + matrix.m22)) / 2;
			quaternion.x  = Mathf.Sqrt(Mathf.Max(0, 1 + matrix.m00 - matrix.m11 - matrix.m22)) / 2;
			quaternion.y  = Mathf.Sqrt(Mathf.Max(0, 1 - matrix.m00 + matrix.m11 - matrix.m22)) / 2;
			quaternion.z  = Mathf.Sqrt(Mathf.Max(0, 1 - matrix.m00 - matrix.m11 + matrix.m22)) / 2;
			quaternion.x *= Mathf.Sign(quaternion.x * (matrix.m21 - matrix.m12));
			quaternion.y *= Mathf.Sign(quaternion.y * (matrix.m02 - matrix.m20));
			quaternion.z *= Mathf.Sign(quaternion.z * (matrix.m10 - matrix.m01));
			return quaternion;
		}


		/// <summary>
		/// Returns the translation part of a 4x4 matrix.
		/// </summary>
		/// <param name="matrix">the matrix to extract the translation from</param>
		/// <returns>the translation part of the matrix</returns>
		/// 
		public static Vector3 GetTranslation(Matrix4x4 matrix)
		{
			vector.x = matrix.m03;
			vector.y = matrix.m13;
			vector.z = matrix.m23;
			return vector;
		}


		/// <summary>
		/// Converts a matrix from right/left handedness to left/right handedness.
		/// http://answers.unity3d.com/storage/temp/12048-lefthandedtorighthanded.pdf
		/// </summary>
		/// <param name="matrix">the matrix to convert in situ</param>
		/// 
		public static void ToggleLeftRightHandedMatrix(ref Matrix4x4 matrix)
		{
			matrix[0,2] = -matrix[0,2];
			matrix[1,2] = -matrix[1,2];
			matrix[2,0] = -matrix[2,0];
			matrix[2,1] = -matrix[2,1];
			matrix[2,3] = -matrix[2,3];
		}


		private static Quaternion quaternion = new Quaternion();
		private static Vector3    vector     = new Vector3();
	}
}
