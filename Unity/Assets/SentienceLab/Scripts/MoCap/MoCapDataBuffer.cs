#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for buffering MoCap data for (e.g., to achieve a delay).
	/// </summary>
	///
	public class MoCapDataBuffer
	{
		/// scene objects associated with this buffer
		public readonly Marker marker;
		public readonly Bone   bone;


		/// <summary>
		/// Creates a new MoCap data buffer object for marker.
		/// </summary>
		/// <param name="marker">marker to associate with this buffer</param>
		/// 
		public MoCapDataBuffer(Marker marker)
		{
			this.marker = marker;
			this.bone   = null;

			// initialise pipeline
			pipeline      = null;
			EnsureCapacity(INITIAL_CAPACITY);
		}


		/// <summary>
		/// Creates a new MoCap data buffer object for marker.
		/// </summary>
		/// <param name="marker">marker to associate with this buffer</param>
		/// 
		public MoCapDataBuffer(Bone bone)
		{
			this.marker = null;
			this.bone   = bone;

			// initialise pipeline
			pipeline      = null;
			EnsureCapacity(INITIAL_CAPACITY);
		}


		/// <summary>
		/// Gets the name of the marker or bone.
		/// </summary>
		/// <returns>name of the marker or bone</returns>
		/// 
		public string GetName()
		{
			return (bone != null) ? bone.name : marker.name;
		}


		/// <summary>
		/// Makes sure the buffer pipeline has at least this amount of elements.
		/// If not, the buffer is enlarged.
		/// </summary>
		/// <param name="minimumCapacity">the minimum amount of elements in the pieline</param>
		/// 
		public void EnsureCapacity(int minimumCapacity)
		{
			if ((pipeline == null) || (minimumCapacity > pipeline.Length))
			{
				// create new buffer
				pipeline = new MoCapData[minimumCapacity];
				for (int i = 0; i < pipeline.Length; i++)
				{
					pipeline[i] = new MoCapData(this);
				}
				pipelineIndex = 0;
				firstPush     = true;
				// Debug.Log("Extended buffer size to " + minimumCapacity + " for " + GetName());
			}
		}


		/// <summary>
		/// Makes sure the buffer pipeline is large enough for operating a chain of modifiers.
		/// If not, the buffer is enlarged.
		/// </summary>
		/// <param name="modifiers">the modifiers to check against</param>
		/// 
		public void EnsureCapacityForModifiers(IMoCapDataModifier[] modifiers)
		{
			foreach (IMoCapDataModifier modifier in modifiers)
			{
				EnsureCapacity(modifier.GetRequiredBufferSize());
			}
		}


		/// <summary>
		/// Push the latest dataset into the buffer pipeline.
		/// </summary>
		/// 
		public void Push()
		{
			if (firstPush)
			{
				// first piece of data > fill the whole pipeline with it
				for (int i = 0; i < pipeline.Length; i++)
				{
					if (bone != null) pipeline[i].Store(bone);
					else              pipeline[i].Store(marker);
				}
				firstPush = false;
			}
			else
			{
				if (bone != null) pipeline[pipelineIndex].Store(bone);
				else              pipeline[pipelineIndex].Store(marker);				
			}

			// advance write index
			pipelineIndex = (pipelineIndex + 1) % pipeline.Length;
		}
	

		/// <summary>
		/// Runs a chain of MoCap data modifiers on the buffer.
		/// </summary>
		/// <param name="modifiers">The array of modifiers to run</param>
		/// <returns>the result of running the modifier chain</returns>
		/// 
		public MoCapData RunModifiers(IMoCapDataModifier[] modifiers)
		{
			MoCapData result = new MoCapData(GetElement(0));
			foreach (IMoCapDataModifier m in modifiers)
			{
				m.Process(ref result);
			}
			return result;
		}


		/// <summary>
		/// Gets a data point from the buffer.
		/// </summary>
		/// <param name="relativeIndex">relative Index (0: newest element, 1: previous element, ...)</param>
		/// <returns>the relative data point in the buffer</returns>
		/// 
		public MoCapData GetElement(int relativeIndex)
		{
			relativeIndex = Mathf.Clamp(relativeIndex, 0, pipeline.Length - 1);
			int idx = pipelineIndex - 1 - relativeIndex;
			if (idx < 0) { idx += pipeline.Length; }
			return pipeline[idx];
		}


		private MoCapData[] pipeline;      // pipeline for the bone data
		private int         pipelineIndex; // current buffer index for writing, index-1 for reading
		private bool        firstPush;     // first push of data flag

		// initial buffer capacity
		private static readonly int INITIAL_CAPACITY = 10;

	}

}
