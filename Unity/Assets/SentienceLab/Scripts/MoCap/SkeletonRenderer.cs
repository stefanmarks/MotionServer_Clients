#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for rendering skeletons for a Motion Capture actor.
	/// </summary>
	///

	[AddComponentMenu("Motion Capture/Skeleton Renderer")]

	public class SkeletonRenderer : MonoBehaviour, SceneListener
	{
		[Tooltip("The name of the MoCap actor to render.")]
		public string actorName;

		[Tooltip("A template game object for how to display the bones. Needs to be one unit long along the Y axis and start at the origin.")]
		public GameObject boneTemplate;


		/// <summary>
		/// Called at the start of the game.
		/// Tries to find the MoCap client singleton and then 
		/// registers this object as a listener with the client.
		/// </summary>
		/// 
		void Start()
		{
			// initialise variables
			skeletonNode = null;
			actor        = null;
			boneList     = new Dictionary<Bone, BoneObject>();

			// sanity checks
			if (boneTemplate == null)
			{
				Debug.LogWarning("No bone template defined");
			}

			// find any MoCap data modifiers and store them
			modifiers = GetComponents<IMoCapDataModifier>();

			// start receiving MoCap data
			MoCapManager.GetInstance().AddSceneListener(this);
		}


		/// <summary>
		/// Creates copies of the bone template for all bones.
		/// </summary>
		/// <param name="bones">bone data from the MoCap system</param>
		/// 
		private void CreateBones(Bone[] bones)
		{
			// create node for containing all the marker objects
			skeletonNode = new GameObject();
			skeletonNode.name = "Bones";
			skeletonNode.transform.parent        = this.transform;
			skeletonNode.transform.localPosition = Vector3.zero;
			skeletonNode.transform.localRotation = Quaternion.identity;
			skeletonNode.transform.localScale    = Vector3.one;

			// create copies of the marker template
			foreach (Bone bone in bones)
			{
				// add empty for position/orientation
				GameObject boneNode = new GameObject();
				boneNode.name = bone.name;

				GameObject boneRepresentation = null;

				if (boneTemplate != null)
				{
					float scale = bone.length;
					if (scale <= 0) { scale = 1; }

					// add subnode for visual that can be scaled
					boneRepresentation = GameObject.Instantiate(boneTemplate);
					boneRepresentation.transform.parent = boneNode.transform;
					boneRepresentation.transform.localScale = scale * Vector3.one;
					boneRepresentation.transform.localRotation = new Quaternion();
					boneRepresentation.name = bone.name + "_visual";
					boneRepresentation.SetActive(true);
				}

				if (bone.parent != null)
				{
					// attach to parent node
					GameObject parentObject = boneList[bone.parent].node;
					boneNode.transform.parent = parentObject.transform;
				}
				else
				{
					// no parent > attach to base skeleton node
					boneNode.transform.parent = skeletonNode.transform;
				}

				boneList[bone] = new BoneObject() { node = boneNode, visual = boneRepresentation };
				bone.buffer.EnsureCapacityForModifiers(modifiers);

				boneTemplate.SetActive(false);
			}
		}


		//// <summary>
		/// Called once per frame.
		/// </summary>
		/// 
		void Update()
		{
			// create marker position array if necessary
			// but only when tracking is OK, otherwise the bone lengths are undefined
			if ((skeletonNode == null) && (actor != null) && actor.bones[0].tracked)
			{
				CreateBones(actor.bones);
			}

			if (skeletonNode == null)
				return;

			// update bones
			foreach (KeyValuePair<Bone, BoneObject> entry in boneList)
			{
				BoneObject  obj  = entry.Value;
				Bone            bone   = entry.Key;
				MoCapData   data = bone.buffer.RunModifiers(modifiers);

				// update bone game object
				if (data.tracked)
				{
					obj.node.transform.localPosition = data.pos;
					obj.node.transform.localRotation = data.rot;

					// update length of representation
					GameObject boneRepresentation = obj.visual;
					if (boneRepresentation != null)
					{
						boneRepresentation.transform.localScale = data.length * Vector3.one;
					}

					obj.node.SetActive(true);

					if (bone.parent != null)
					{
						Debug.DrawLine(
							obj.node.transform.parent.position, 
							obj.node.transform.position, 
							Color.red);
					}
				}
				else
				{
					// bone not tracked anymore
					obj.node.SetActive(false);
				}
			}
		}


		public void SceneDataUpdated(Scene scene)
		{
			// nothing to do here
		}


		public void SceneDefinitionChanged(Scene scene)
		{
			// actor has changed > rebuild skeleton on next update
			if (skeletonNode != null)
			{
				// if necessary, destroy old container
				GameObject.Destroy(skeletonNode);
				skeletonNode = null;
			}

			actor = scene.FindActor(actorName);
			if (actor != null)
			{
				Debug.Log("Skeleton Renderer '" + this.name + "' controlled by MoCap actor '" + actorName + "'.");
			}
			else
			{
				Debug.LogWarning("Skeleton Renderer '" + this.name + "' cannot find MoCap actor '" + actorName + "'.");
			}
		}


		struct BoneObject
		{
			public GameObject node;
			public GameObject visual;
		}
		private GameObject                        skeletonNode;
		private Actor                             actor;
		private Dictionary<Bone, BoneObject> boneList;
		private IMoCapDataModifier[]              modifiers; // list of modifiers for this renderer

	}

}
