using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// FIFO storage structure for delaying MoCap data.
/// </summary>
/// 
namespace MoCap
{
	/// <summary>
	/// Class for buffering MoCap data for (e.g., to achieve a delay).
	/// </summary>
	/// 
	public class MoCapDataBuffer
	{
		/// <summary>
		/// Class for buffering MoCap data (e.g., .
		/// </summary>
		/// 
		public class MoCapData
		{
			public Vector3 pos;
			public Quaternion rot;
			public bool tracked;


			/// <summary>
			/// Creates a new MoCap data object.
			/// </summary>
			/// 
			public MoCapData()
			{
				pos = new Vector3();
				rot = new Quaternion();
				tracked = false;
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
			}
		}


		/// <summary>
		/// Creates a new MoCap data buffer object
		/// </summary>
		/// <param name="obj">game object to associate with this buffer</param>
		/// <param name="delay">the delay in seconds to implement with the buffer</param>
		/// 
		public MoCapDataBuffer(GameObject obj, float delay)
		{
			int delayInFrames = Mathf.Max(1, 1 + (int)(delay * 60)); // TODO: Find out or define framerate somewhere central
			pipeline = new MoCapData[delayInFrames];
			for (int i = 0; i < pipeline.Length; i++)
			{
				pipeline[i] = new MoCapData();
			}
			index = 0;
			firstPush = true;

			gameObject = obj;
		}


		/// <summary>
		/// Put a marker dataset into the pipeline
		/// and extract the delayed dataset.
		/// </summary>
		/// <param name="marker">the marker data to add into the queue</param>
		/// <returns>the delayed dataset</returns>
		/// 
		public MoCapData Process(Marker marker)
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
				pipeline[index].Store(marker);
			}
			index = (index + 1) % pipeline.Length;
			return pipeline[index];
		}


		/// <summary>
		/// Put a bone dataset into the pipeline
		/// and extract the delayed dataset.
		/// </summary>
		/// <param name="bone">the bone data to add into the queue</param>
		/// <returns>the delayed dataset</returns>
		/// 
		public MoCapData Process(Bone bone)
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
				pipeline[index].Store(bone);
			}
			index = (index + 1) % pipeline.Length;
			return pipeline[index];
		}


		/// <summary>
		/// Gets the associated game object.
		/// </summary>
		/// <returns>the associated game object</returns>
		/// 
		public GameObject GetGameObject()
		{
			return gameObject;
		}


		private MoCapData[] pipeline;   // pipeline for the bone data
		private int         index;      // current buffer index for writing, index-1 for reading
		private bool        firstPush;  // first push of data flag
		private GameObject  gameObject; // game object associated with this buffer
	}
}
