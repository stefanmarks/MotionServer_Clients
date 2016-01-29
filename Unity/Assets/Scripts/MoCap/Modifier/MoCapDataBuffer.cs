using UnityEngine;
using System.Collections.Generic;

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
			public Vector3    pos; // position of marker or bone
			public Quaternion rot; // orientation of bone
			public bool  tracked;  // tracking flag
			public float length;   // length of bone


			/// <summary>
			/// Creates a new MoCap data object.
			/// </summary>
			/// 
			public MoCapData()
			{
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


		/// <summary>
		/// Interface for components that influence MoCap data, e.g., scaling, mirroring
		/// </summary>
		/// 
		public interface Manipulator
		{
			/// <summary>
			/// Processes a MoCap data point.
			/// </summary>
			/// <param name="data">data point to be modified</param>
			/// 
			void Process(ref MoCapData data);
		}


		/// <summary>
		/// Creates a new MoCap data buffer object
		/// </summary>
		/// <param name="owner">game object that owns this buffer</param>
		/// <param name="obj">game object to associate with this buffer</param>
		/// <param name="data">arbitrary object to associate with this buffer</param>
		/// 
		public MoCapDataBuffer(GameObject owner, GameObject obj, System.Object data = null)
		{
			// find any manipulators and store them
			manipulators = owner.GetComponents<Manipulator>();

			// specifically find the delay manipulator and set the FIFO size accordingly
			MoCapData_Delay delayComponent = owner.GetComponent<MoCapData_Delay>();
			float delay = (delayComponent != null) ? delayComponent.delay : 0;
			int   delayInFrames = Mathf.Max(1, 1 + (int)(delay * 60)); // TODO: Find out or define framerate somewhere central
			pipeline = new MoCapData[delayInFrames];
			for (int i = 0; i < pipeline.Length; i++)
			{
				pipeline[i] = new MoCapData();
			}
			index = 0;

			firstPush = true;
			gameObject = obj;
			dataObject = data;
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

			// manipulate data before returning
			MoCapData retValue = pipeline[index];
			foreach ( Manipulator m in manipulators )
			{
				m.Process(ref retValue);
			}

			return retValue;
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

			// manipulate data before returning
			MoCapData retValue = pipeline[index];
			foreach (Manipulator m in manipulators)
			{
				m.Process(ref retValue);
			}

			return retValue;
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


		/// <summary>
		/// Gets the associated data object.
		/// </summary>
		/// <returns>the associated data object</returns>
		/// 
		public System.Object GetDataObject()
		{
			return dataObject;
		}


		private MoCapData[]   pipeline;     // pipeline for the bone data
		private Manipulator[] manipulators; // list of manipulators for this buffer
		private int           index;        // current buffer index for writing, index-1 for reading
		private bool          firstPush;    // first push of data flag
		private GameObject    gameObject;   // game object associated with this buffer
		private System.Object dataObject;   // arbitrary object associated with this buffer
	}
}
