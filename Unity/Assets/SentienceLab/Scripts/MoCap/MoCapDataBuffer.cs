#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Threading;
using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for buffering MoCap data for (e.g., to achieve a delay).
	/// </summary>
	///
	public class MoCapDataBuffer
	{
		/// name of this buffer (= marker or bone name)
		public readonly string Name;

		/// game object associated with this buffer
		public readonly GameObject GameObject;

		/// arbitrary object associated with this buffer
		public readonly System.Object DataObject;


		/// <summary>
		/// Creates a new MoCap data buffer object.
		/// </summary>
		/// <param name="name">name of this buffer</param>
		/// <param name="owner">game object that owns this buffer</param>
		/// <param name="obj">game object to associate with this buffer</param>
		/// <param name="data">arbitrary object to associate with this buffer</param>
		/// 
		public MoCapDataBuffer(string name, GameObject owner, GameObject obj, System.Object data = null)
		{

			this.Name = name;
			this.GameObject = obj;
			this.DataObject = data;

			// initialise pipeline
			pipelineMutex = new Mutex();
			pipeline      = null;
			EnsureCapacity(10);
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
				pipelineMutex.WaitOne();
				pipeline = new MoCapData[minimumCapacity];
				for (int i = 0; i < pipeline.Length; i++)
				{
					pipeline[i] = new MoCapData(this);
				}
				pipelineIndex = 0;
				firstPush     = true;
				pipelineMutex.ReleaseMutex();
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
		/// Push a marker dataset into the buffer pipeline.
		/// </summary>
		/// <param name="marker">the marker data to add into the buffer</param>
		/// 
		public void Push(Marker marker)
		{
			if (firstPush)
			{
				// first piece of data > fill the whole pipeline with it
				for (int i = 0; i < pipeline.Length; i++)
				{
					pipeline[i].Store(marker);
				}
				firstPush = false;
			}
			else
			{
				pipeline[pipelineIndex].Store(marker);
			}

			// advance write index
			pipelineMutex.WaitOne();
			pipelineIndex = (pipelineIndex + 1) % pipeline.Length;
			pipelineMutex.ReleaseMutex();
		}


		/// <summary>
		/// Push a bone dataset into the buffer pipeline.
		/// </summary>
		/// <param name="bone">the bone data to add into the buffer</param>
		/// 
		public void Push(Bone bone)
		{
			if (firstPush)
			{
				// first piece of data > fill the whole pipeline with it
				for (int i = 0; i < pipeline.Length; i++)
				{
					pipeline[i].Store(bone);
				}
				firstPush = false;
			}
			else
			{
				pipeline[pipelineIndex].Store(bone);
			}

			// advance write index
			pipelineMutex.WaitOne();
			pipelineIndex = (pipelineIndex + 1) % pipeline.Length;
			pipelineMutex.ReleaseMutex();
		}
	

		/// <summary>
		/// Runs a chain of MoCap data modifiers on the buffer.
		/// </summary>
		/// <param name="modifiers">The array of modifiers to run</param>
		/// <returns>the result of running the modifier chain</returns>
		/// 
		public MoCapData RunModifiers(IMoCapDataModifier[] modifiers)
		{
			pipelineMutex.WaitOne();

			MoCapData result = new MoCapData(GetElement(0));
			foreach (IMoCapDataModifier m in modifiers)
			{
				m.Process(ref result);
			}

			pipelineMutex.ReleaseMutex();

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
		private Mutex       pipelineMutex; // mutex around pipeline
		private bool        firstPush;     // first push of data flag
	}

}
