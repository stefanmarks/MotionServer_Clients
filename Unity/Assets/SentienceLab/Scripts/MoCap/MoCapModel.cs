#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using System.Collections.Generic;

namespace SentienceLab.MoCap
{
	[System.Serializable]
	public class BoneNameTranslationEntry
	{
		public string nameMocap;           // name of the bone in the MoCap skeleton
		public string nameModel;           // name of the bone in the Model
		public string axisTransformation;  // axis transformation to be applied
	}


	/// <summary>
	/// Class for controlling a rigged model via MoCap data.
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/MoCap Model")]

	public class MoCapModel : MonoBehaviour, SceneListener
	{
		[Tooltip("The name of the MoCap actor to link to this model.")]
		public string actorName;

		[Tooltip("Prefix to add to the bone names before searching in the hierarchy.")]
		public string boneNamePrefix = "";

		[Tooltip("What components of the MoCap data stream to use.")]
		public TrackingUsage trackingUsage = TrackingUsage.RotationOnly;

		[Tooltip("Table for translating MoCap bone names to Model bone names.")]
		public BoneNameTranslationEntry[] boneNameTranslationTable = new BoneNameTranslationEntry[0];


		/// <summary>
		/// What data to use from the MoCap system.
		/// </summary>
		/// 
		public enum TrackingUsage
		{
			/// <summary>
			/// Use position and rotation.
			/// </summary>
			PositionAndRotation,

			/// <summary>
			/// Use only rotation
			/// </summary>
			RotationOnly
		};


		/// <summary>
		/// Called at the start of the game.
		/// Tries to find the MoCap client singleton and then 
		/// registers this object as a listener with the client.
		/// </summary>
		/// 
		void Start()
		{
			dataBuffers = null;

			// find any MoCap data modifiers and store them
			modifiers = GetComponents<IMoCapDataModifier>();

			MoCapManager.GetInstance().AddSceneListener(this);
		}


		/// <summary>
		/// Finds the rigging in the model and matches it to the MoCap bone system.
		/// </summary>
		/// <param name="bones">bone data from the MoCap system</param>
		/// 
		private void MatchBones(Bone[] bones)
		{
			dataBuffers = new Dictionary<Bone, MoCapDataBuffer>();
			string unmatchedBones = "";

			// create copies of the marker template
			foreach (Bone bone in bones)
			{
				BoneNameTranslationEntry entry = null;
				string boneName = boneNamePrefix + TranslateBoneName(bone.name, ref entry);
				// find the child in the model with the given bone name
				Transform boneNode = Utilities.FindInHierarchy(boneName, transform);
				if (boneNode != null)
				{
					dataBuffers[bone] = new MoCapDataBuffer(bone.name, this.gameObject, boneNode.gameObject, entry);
					dataBuffers[bone].EnsureCapacityForModifiers(modifiers);
				}
				else
				{
					if (unmatchedBones.Length > 0)
					{
						unmatchedBones += ", ";
					}
					unmatchedBones += bone.name;
				}
			}

			if (unmatchedBones.Length > 0)
			{
				Debug.LogWarning("Could not find the following bones in Model '" + this.name + "': " + unmatchedBones);
			}
		}


		/// <summary>
		/// Translates a bone name form the MoCap name into the Model name
		/// if defined in the table.
		/// </summary>
		/// <param name="name">the name in the MoCap scene</param>
		/// <returns>the translated name for the model</returns>
		/// 
		private string TranslateBoneName(string name, ref BoneNameTranslationEntry actualEntry)
		{
			string translatedName = "undefined";
			foreach (BoneNameTranslationEntry entry in boneNameTranslationTable)
			{
				if (name == entry.nameMocap)
				{
					if (entry.nameModel.Length > 0)
					{
						translatedName = entry.nameModel;
					}
					else
					{
						translatedName = name;
					}
					actualEntry = entry;
					break;
				}
			}
			return translatedName;
		}


		//// <summary>
		/// Called once per frame. Updates the model based on the bone rotations and positions.
		/// </summary>
		/// 
		void Update()
		{
			if (dataBuffers == null)
				return;

			// update bones
			Quaternion rot = new Quaternion();
			foreach (KeyValuePair<Bone, MoCapDataBuffer> entry in dataBuffers)
			{
				Bone            bone   = entry.Key;
				MoCapDataBuffer buffer = entry.Value;
				GameObject      obj    = buffer.GameObject;
				MoCapData       data   = buffer.RunModifiers(modifiers);

				// update bone game object
				if (data.tracked)
				{
					if ( (trackingUsage == TrackingUsage.PositionAndRotation) ||
						 (bone.parent == null) )
					{
						// change position only when desired, or when a root bone
						obj.transform.localRotation = Quaternion.identity;
						obj.transform.localPosition = data.pos;
					}

					rot = Quaternion.identity;

					string transforms = ((BoneNameTranslationEntry)buffer.DataObject).axisTransformation;
					foreach (char c in transforms)
					{
						switch (c)
						{
							case 'X': rot *= Quaternion.Euler(90, 0, 0); break;
							case 'x': rot *= Quaternion.Euler(-90, 0, 0); break;
							case 'Y': rot *= Quaternion.Euler(0, 90, 0); break;
							case 'y': rot *= Quaternion.Euler(0, -90, 0); break;
							case 'Z': rot *= Quaternion.Euler(0, 0, 90); break;
							case 'z': rot *= Quaternion.Euler(0, 0, -90); break;
						}
					}

					obj.transform.localRotation = data.rot * rot;
				}
			}
		}


		public void SceneUpdated(Scene scene)
		{
			// create marker position array if necessary
			// but only when tracking is OK, otherwise the bone lengths are undefined
			if ((dataBuffers == null) && (actor != null))
			{
				MatchBones(actor.bones);
			}

			// update bone data
			foreach (KeyValuePair<Bone, MoCapDataBuffer> entry in dataBuffers)
			{
				Bone            bone   = entry.Key;
				MoCapDataBuffer buffer = entry.Value;
				// pump bone data through buffer
				buffer.Push(bone);
			}
		}


		public void SceneChanged(Scene scene)
		{
			// re-match bone names with the next update
			dataBuffers = null;

			actor = scene.FindActor(actorName);
			if (actor != null)
			{
				Debug.Log("MoCapModel '" + this.name + "' controlled by MoCap actor '" + actorName + "'.");
			}
			else
			{
				Debug.LogWarning("MoCapModel '" + this.name + "' cannot find MoCap actor '" + actorName + "'.");
			}
		}

		private Actor                             actor;
		private Dictionary<Bone, MoCapDataBuffer> dataBuffers;
		private IMoCapDataModifier[]              modifiers; // list of modifiers for this renderer
	}
}
