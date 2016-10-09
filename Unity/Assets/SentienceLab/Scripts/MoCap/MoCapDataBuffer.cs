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
			// find any manipulators and store them
			modifiers = owner.GetComponents<IModifier>();

			// specifically find the delay manipulator and set the FIFO size accordingly
			DelayModifier delayComponent = owner.GetComponent<DelayModifier>();
			float delay = (delayComponent != null) ? delayComponent.delay : 0;
			int   delayInFrames = Mathf.Max(1, 1 + (int)(delay * 60)); // TODO: Find out or define framerate somewhere central
			pipeline = new MoCapData[delayInFrames];
			for (int i = 0; i < pipeline.Length; i++)
			{
				pipeline[i] = new MoCapData(this);
			}
			index = 0;

			firstPush       = true;
			this.Name       = name;
			this.GameObject = obj;
			this.DataObject = data;
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
			foreach (IModifier m in modifiers)
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
			foreach (IModifier m in modifiers)
			{
				m.Process(ref retValue);
			}

			return retValue;
		}


		private MoCapData[]   pipeline;     // pipeline for the bone data
		private IModifier[]   modifiers;    // list of modifiers for this buffer
		private int           index;        // current buffer index for writing, index-1 for reading
		private bool          firstPush;    // first push of data flag
	}

}
