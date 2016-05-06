using UnityEngine;
using System.Collections.Generic;

namespace MoCap
{
	/// <summary>
	/// Class for controlling a game object by Motion Capture data.
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/MoCap Object")]

	public class MoCapObject : MonoBehaviour, ActorListener
	{
		[Tooltip("The name of the MoCap actor to link to this game object.")]
		public string actorName = "";

		[Tooltip("The name of the bone to link to this game object (Empty: Use root bone)")]
		public string boneName = "";

		[Tooltip("What components of the MoCap data stream to use.")]
		public TrackingUsage trackingUsage = TrackingUsage.PositionAndRotation;

		[Tooltip("What to do when tracking of the actor is lost.")]
		public TrackingLostBehaviour trackingLostBehaviour = TrackingLostBehaviour.Disable;


		/// <summary>
		/// Possible actions for when a markerset loses tracking.
		/// </summary>
		public enum TrackingLostBehaviour
		{
			/// <summary>
			/// Freeze the object at the last tracked position/orientation.
			/// </summary>
			Freeze,

			/// <summary>
			/// Disable the game object and re-enable when tracking continues.
			/// </summary>
			Disable
		};


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
			/// use only position.
			/// </summary>
			PositionOnly,

			/// <summary>
			/// Use only rotation
			/// </summary>
			RotationOnly
		};


		/// <summary>
		/// Called at the beginning of the program execution.
		/// </summary>
		///
		void Start()
		{
			// initialise variables
			rootNode        = null;
			controllingBone = null;
			dataBuffers     = new Dictionary<Bone, MoCapDataBuffer>();

			MoCapClient.GetInstance().AddActorListener(this);
		}


		/// <summary>
		/// Creates a hierarchy of a selected bone of an actor.
		/// </summary>
		/// <param name="actor">actor to use</param>
		/// 
		private void CreateHierarchy(Actor actor)
		{
			// create node for containing all the hierarchy objects
			rootNode = new GameObject();
			rootNode.name = this.name + "_Root";
			rootNode.transform.parent = this.transform.parent;
			rootNode.transform.localPosition = Vector3.zero;
			rootNode.transform.localRotation = Quaternion.identity;
			rootNode.transform.localScale = Vector3.one;

			// create hierarchy
			GameObject boneNode = rootNode;
			foreach (Bone bone in controllingBone.chain)
			{
				// add empty for position/orientation
				boneNode = new GameObject();
				boneNode.name = bone.name;

				if (bone.parent != null)
				{
					// attach to parent node
					GameObject parentObject = dataBuffers[bone.parent].GameObject;
					boneNode.transform.parent = parentObject.transform;
				}
				else
				{
					// no parent = root bone > attach to root node
					boneNode.transform.parent = rootNode.transform;
				}
				boneNode.transform.localScale = Vector3.one;

				dataBuffers[bone] = new MoCapDataBuffer(bone.name, this.gameObject, boneNode);
			}

			// move this transform to the end of the hierarchy
			this.transform.parent        = boneNode.transform;
			this.transform.localPosition = Vector3.zero;
			this.transform.localRotation = Quaternion.identity;
		}


		//// <summary>
		/// Called once per frame.
		/// </summary>
		/// 
		void Update()
		{
			if (controllingBone == null)
				return;

			// update bones
			foreach (KeyValuePair<Bone, MoCapDataBuffer> entry in dataBuffers)
			{
				Bone            bone   = entry.Key;
				MoCapDataBuffer buffer = entry.Value;
				GameObject      obj    = buffer.GameObject;

				// pump data through buffer
				MoCapData data = buffer.Process(bone);

				// update hierarchy object
				if (data.tracked)
				{
					if ( (trackingUsage == TrackingUsage.RotationOnly) ||
						 (trackingUsage == TrackingUsage.PositionAndRotation))
					{
						obj.transform.localRotation = data.rot;
					}
					if ( (trackingUsage == TrackingUsage.PositionOnly) ||
						 (trackingUsage == TrackingUsage.PositionAndRotation))
					{
						obj.transform.localPosition = data.pos;
					}
					obj.SetActive(true);
				}
				else
				{
					// bone not tracked anymore, freeze or disable
					if (trackingLostBehaviour == TrackingLostBehaviour.Disable)
					{
						obj.SetActive(false);
					}
				}
			}
		}


		/// <summary>
		/// Gets the name of the actor.
		/// </summary>
		/// <returns>The name of the actor</returns>
		/// 
		public string GetActorName()
		{
			return actorName;
		}


		/// <summary>
		/// Callback for the MoCap client when new data has arrived.
		/// </summary>
		/// <param name="actor">the actor that has been updated</param>
		/// 
		public void ActorUpdated(Actor actor)
		{
			// create node hierarchy if not already built.
			// but only when tracking is OK, otherwise the bone lengths are undefined
			if (rootNode == null)
			{
				CreateHierarchy(actor);
			}
			if (!this.gameObject.activeInHierarchy)
			{
				// game object is inactive, so Update() doesn't get called any more
				// which would mean once deactivated, the object stays hidden forever
				// Workaround > call from here
				Update();
			}
		}


		public void ActorChanged(Actor actor)
		{
			// actor has changed > rebuild hierarchy on next update
			if (rootNode != null)
			{
				// if necessary, destroy old container
				this.transform.parent = rootNode.transform.parent;
				GameObject.Destroy(rootNode);
				rootNode = null;
			}

			if (actor != null)
			{
				if (boneName.Length > 0)
				{
					controllingBone = actor.FindBone(boneName);
				}
				if (controllingBone == null)
				{
					// not found, or not defined > use root bone
					Debug.Log("MoCap Object '" + this.name + "' controlled by MoCap actor '" + actorName + "'.");
					controllingBone = actor.bones[0];
				}
				else
				{
					Debug.Log("MoCap Object '" + this.name + "' controlled by MoCap actor/bone '" +
						actorName + "/" + controllingBone.name + "'.");
				}
			}
			else
			{
				Debug.LogWarning("Mocap Object '" + this.name + "' cannot find MoCap actor '" + actorName + "'.");
			}
		}


		private Bone                              controllingBone;
		private GameObject                        rootNode;
		private Dictionary<Bone, MoCapDataBuffer> dataBuffers;
	}

}
