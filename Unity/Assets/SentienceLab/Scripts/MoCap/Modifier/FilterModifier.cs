#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Component for filtering MoCap data (e.g., smoothing).
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/Modifier/Filter")]

	public class FilterModifier : MonoBehaviour, IMoCapDataModifier
	{
		[Tooltip("Filtering type")]
		public FilterType filterType = FilterType.Rectangle;

		[Tooltip("Filtering time in seconds")]
		[Range(0, 10)]
		public float filterTime = 1.0f;

		[Tooltip("Maximum number of filter elements (0: calculate from filter size")]
		[Range(0, 512)]
		public int maximumFilterSize = 0;


		public enum FilterType
		{
			Rectangle,
			Gaussian,
			LowPass
		}


		public void Start()
		{
			// calculate maximum filter size if necessary
			if (maximumFilterSize <= 0)
			{
				maximumFilterSize = (int) (filterTime * MoCapManager.GetInstance().GetFramerate());
				maximumFilterSize = Mathf.Max(maximumFilterSize, 1);
			}

			InitialiseFilter();
		}


		/// <summary>
		/// Builds the FIR filter weights depending on size and filter type.
		/// </summary>
		/// 
		private void InitialiseFilter()
		{
			float framerate = MoCapManager.GetInstance().GetFramerate();
			int filterSize = Mathf.Clamp((int) (framerate * filterTime), 1, maximumFilterSize);
			filter = new float[filterSize];
			for (int i = 0; i < filterSize; i++)
			{
				float x  = (i - (filterSize / 2.0f)) / framerate;
				switch (filterType)
				{
					case FilterType.Rectangle:
						filter[i] = 1.0f;
						break;

					case FilterType.Gaussian:
						float d2 = 0.25f * 0.25f; // squared standard deviation range
						filter[i] = 1.0f / (2.0f * Mathf.PI * d2) * Mathf.Exp(-(x * x) / (2.0f * d2));
						break;

					case FilterType.LowPass:
						x *= Mathf.PI * 2;
						x += 0.00001f; // avoid division by zero
						filter[i] = Mathf.Sin(x)/x;
						break;
				}
			}
			oldFilterTime = filterTime;
		}


		/// <summary>
		/// Filtering of mocap data with a somewhat crude approach to handling quaternions
		/// as described in  
		/// </summary>
		/// <param name="data">the MoCap data item to process</param>
		/// 
		public void _Process(ref MoCapData data)
		{
			if (!enabled) return;

			// has the filter time changed?
			if (filterTime != oldFilterTime) InitialiseFilter();

			Vector3 pos = Vector3.zero;
			float qx, qy, qz, qw; qx = qy = qz = qw = 0;
			float length = 0;
			MoCapData first = data;

			float factorSum = 0;
			bool  firstEntry = true;
			for ( int idx = 0; idx < filter.Length; idx++ )
			{
				MoCapData d = data.buffer.GetElement(idx);
				if ( d.tracked )
				{
					// determine first valid entry
					if (firstEntry)
					{
						first = d;
						firstEntry = false;
					}

					// position and length: standard application of FIR filter
					float factor = filter[idx];
					factorSum += factor;

					pos += d.pos * factor;
					length += d.length * factor;

					// quaternions: consider q = -q condition by checking dot product with first element
					float dot = first.rot.x * d.rot.x + first.rot.y * d.rot.y + first.rot.z * d.rot.z + first.rot.w * d.rot.w;
					if (dot < 0) { factor = -factor; }
					qx += d.rot.x * factor;
					qy += d.rot.y * factor;
					qz += d.rot.z * factor;
					qw += d.rot.w * factor;
				}
			}

			// done adding up: now "normalize"
			if (factorSum > 0)
			{
				pos /= factorSum;
				float mag = Mathf.Sqrt(qx * qx + qy * qy + qz * qz + qw * qw);
				qx /= mag; qy /= mag; qz /= mag; qw /= mag;
				length /= factorSum;
			}

			// and write back into result
			data.pos = pos;
			data.rot.Set(qx, qy, qz, qw);
			data.tracked = !firstEntry;
			data.length  = length;
		}


		/// <summary>
		/// Filtering of MoCap data using the matrix eigenvalue approach described in 
		/// https://github.com/tolgabirdal/averaging_quaternions/blob/master/wavg_quaternion_markley.m
		/// Eigenvalue calculation using the Power Iteration algorithm described in
		/// http://www.bragitoff.com/2015/10/eigen-value-and-eigen-vector-of-a-matrix-by-iterative-method-c-program/
		/// </summary>
		/// <param name="data">the MoCap data item to process</param>
		/// 
		public void Process(ref MoCapData data)
		{
			if (!enabled) return;

			// has the filter time changed?
			if (filterTime != oldFilterTime) InitialiseFilter();

			Vector3   pos    = Vector3.zero;
			Matrix4x4 rot    = Matrix4x4.zero;
			float     length = 0;
			MoCapData first  = data;
			Vector4   v      = Vector4.zero;

			float factorSum  = 0;
			bool  firstEntry = true;
			for (int idx = 0; idx < filter.Length; idx++)
			{
				MoCapData d = data.buffer.GetElement(idx);
				if (d.tracked)
				{
					// determine first valid entry
					if (firstEntry)
					{
						first = d;
						firstEntry = false;
					}

					// position and length: standard application of FIR filter
					float factor = filter[idx];
					factorSum += factor;

					pos    += d.pos * factor;
					length += d.length * factor;

					// quaternions: build up matrix
					Add(ref rot, ref d.rot, factor);
				}
			}

			// done adding up: now "normalize"
			if (factorSum > 0)
			{
				pos /= factorSum;
				for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) rot[i, j] /= factorSum;
				length /= factorSum;
			}

			// and write back into result
			data.pos     = pos;
			data.tracked = !firstEntry;
			data.length  = length;
			// average quaterion is eigenvector of accumulated matrix
			v.Set(first.rot.x, first.rot.w, first.rot.z, first.rot.w);
			v = FindEigenvector(rot, v);
			data.rot.Set(v.x, v.y, v.z, v.w);
		}


		/// <summary>
		/// Adds weight * (q * qT) to a matrix
		/// </summary>
		/// <param name="mtx">Matrix to add to</param>
		/// <param name="q">Quaterion to add</param>
		/// <param name="weight">weight</param>
		/// 
		public void Add(ref Matrix4x4 mtx, ref Quaternion q, float weight)
		{
			// diagonal
			mtx.m00 += weight * q.x * q.x;
			mtx.m11 += weight * q.y * q.y;
			mtx.m22 += weight * q.z * q.z;
			mtx.m33 += weight * q.w * q.w;

			// corners and transpose
			mtx.m01 += weight * q.x * q.y; mtx.m10 = mtx.m01;
			mtx.m02 += weight * q.x * q.z; mtx.m20 = mtx.m02;
			mtx.m03 += weight * q.x * q.w; mtx.m30 = mtx.m03;

			mtx.m12 += weight * q.y * q.z; mtx.m21 = mtx.m12;
			mtx.m13 += weight * q.y * q.w; mtx.m31 = mtx.m13;

			mtx.m23 += weight * q.z * q.w; mtx.m32 = mtx.m23;
		}


		/// <summary>
		/// Finds the primary eigenvector of a 4x4 matrix using Power Iteration.
		/// https://en.wikipedia.org/wiki/Power_iteration
		/// based on http://www.bragitoff.com/2015/10/eigen-value-and-eigen-vector-of-a-matrix-by-iterative-method-c-program/
		/// </summary>
		/// 
		public Vector4 FindEigenvector(Matrix4x4 mtx, Vector4 startGuess, double epsilon = 0.0001f)
		{
			Vector4 eigen = startGuess;
			float   k     = eigen.x;
			float   y;
			do
			{
				y = k;
				// c = mtx * eigen
				Vector4 c = mtx * eigen;
				// find larges element of c
				k = Mathf.Max(
					Mathf.Max(Mathf.Abs(c.x), Mathf.Abs(c.y)),
					Mathf.Max(Mathf.Abs(c.z), Mathf.Abs(c.w)));
				// calculate the new eigenvector
				eigen = c / k;                
			} while (Mathf.Abs(k - y) > epsilon);

			return eigen;
		}


		public int GetRequiredBufferSize()
		{
			return maximumFilterSize;
		}


		private float   oldFilterTime;
		private float[] filter;
	}

}
