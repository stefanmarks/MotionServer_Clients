#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for controlling a game object by Motion Capture data.
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/MoCap Object")]

	public class MoCapObject : MonoBehaviour, SceneListener
	{
		[Tooltip("The name of the MoCap actor to link to this game object.")]
		public string actorName = "";

		[Tooltip("The name of the bone to link to this game object (Empty: Use root bone)")]
		public string boneName = "";

		[Tooltip("What components of the MoCap data stream to use.")]
		public TrackingUsage trackingUsage = TrackingUsage.PositionAndRotation;

		[Tooltip("What to do when tracking of the actor is lost.")]
		public TrackingLostBehaviour trackingLostBehaviour = TrackingLostBehaviour.Disable;

		[Tooltip("When to update the object")]
		public UpdateType updateType = UpdateType.Update;


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
		/// When to do the object update.
		/// </summary>
		public enum UpdateType
		{
			Update,
			PreRender,
			UpdateAndPreRender
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
			boneList        = new Dictionary<Bone, GameObject>();
			disabled        = false;

			notFoundWarningIssued = false;

			// find any MoCap data modifiers and store them
			modifiers = GetComponents<IMoCapDataModifier>();

			// let the MoCap manager handle the forced Update calls
			MoCapManager.GetInstance().StartCoroutine(ForceUpdateCall(this));

			MoCapManager.GetInstance().AddSceneListener(this);
		}


		private IEnumerator ForceUpdateCall(MoCapObject o)
		{
			// When the game object is inactive, Update() doesn't get called any more
			// which would mean once deactivated, the object stays hidden forever
			// Workaround > when deactivated, call from here
			while (true)
			{
				if (o.disabled) o.Update();
				yield return new WaitForSeconds(0.1f);
			}
		}


		/// <summary>
		/// Creates a hierarchy of a selected bone of an actor.
		/// </summary>
		/// 
		private void CreateHierarchy()
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
					GameObject parentObject = boneList[bone.parent];
					boneNode.transform.parent = parentObject.transform;
				}
				else
				{
					// no parent = root bone > attach to root node
					boneNode.transform.parent = rootNode.transform;
				}
				boneNode.transform.localScale = Vector3.one;

				boneList[bone] = boneNode;
				bone.buffer.EnsureCapacityForModifiers(modifiers);
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
		public void Update()
		{
			if ( (updateType == UpdateType.Update) || 
			     (updateType == UpdateType.UpdateAndPreRender) )
			{
				MoCapManager.GetInstance().Update();
				UpdateObject();
			}
		}


		//// <summary>
		/// Called just before the frame renders.
		/// </summary>
		/// 
		public void OnPreRender()
		{
			if ( (updateType == UpdateType.PreRender) ||
			     (updateType == UpdateType.UpdateAndPreRender))
			{
				MoCapManager.GetInstance().OnPreRender();
				UpdateObject();
			}
		}


		private void UpdateObject()
		{
			// create node hierarchy if not already built.
			// but only when tracking is OK, otherwise the bone lengths are undefined
			if ((rootNode == null) && (controllingBone != null))
			{
				CreateHierarchy();
			}

			if (controllingBone == null)
				return;

			// update bones
			foreach (KeyValuePair<Bone, GameObject> pair in boneList)
			{
				GameObject obj  = pair.Value;
				Bone       bone = pair.Key;
				MoCapData  data = bone.buffer.RunModifiers(modifiers);

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
					disabled = false;
				}
				else
				{
					// bone not tracked anymore, freeze or disable
					if (trackingLostBehaviour == TrackingLostBehaviour.Disable)
					{
						obj.SetActive(false);
						disabled = true;
					}
				}
			}
		}


		public void SceneDataUpdated(Scene scene)
		{
			// nothing to do here
		}


		public void SceneDefinitionChanged(Scene scene)
		{
			// actor has changed > rebuild hierarchy on next update
			if (rootNode != null)
			{
				// if necessary, destroy old container
				this.transform.parent = rootNode.transform.parent;
				GameObject.Destroy(rootNode);
				rootNode = null;
			}

			Actor actor = scene.FindActor(actorName);
			if (actor != null)
			{
				if (boneName.Length > 0)
				{
					controllingBone = actor.FindBone(boneName);
				}
				if (controllingBone == null)
				{
					// not found, or not defined > use root bone
					Debug.Log("MoCap Object '" + this.name + "' controlled by MoCap actor '" + actor.name + "'.");
					controllingBone = actor.bones[0];
				}
				else
				{
					Debug.Log("MoCap Object '" + this.name + "' controlled by MoCap actor/bone '" +
						actor.name + "/" + controllingBone.name + "'.");
				}
				this.gameObject.SetActive(true);
			}
			else
			{
				if (!notFoundWarningIssued)
				{
					Debug.LogWarning("Mocap Object '" + this.name + "' cannot find MoCap actor '" + actorName + "'.");
					notFoundWarningIssued = true; // shut up now
				}
				if (trackingLostBehaviour == TrackingLostBehaviour.Disable)
				{
					// MoCap Actor not found at all > time to disable object?
					this.gameObject.SetActive(false);
				}
			}
		}


		private Bone                         controllingBone;
		private GameObject                   rootNode;
		private Dictionary<Bone, GameObject> boneList;
		private bool                         disabled;
		private IMoCapDataModifier[]         modifiers; // list of modifiers for this renderer
		private bool                         notFoundWarningIssued;
	}

}
