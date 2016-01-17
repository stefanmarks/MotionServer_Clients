using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoCap;

/// <summary>
/// Class for rendering skeletons for a Motion Capture actor.
/// </summary>
///
[DisallowMultipleComponent]
[AddComponentMenu("Motion Capture/Skeleton Renderer")]
public class SkeletonRenderer : MonoBehaviour, ActorListener
{
	[Tooltip("The name of the MoCap actor to link to this renderer.")]
	public string actorName;

	[Tooltip("A template game object for how to display the bones. Needs to be one unit long along the Y axis and start at the origin.")]
	public GameObject boneTemplate;

	[Tooltip("Delay of the rendering in seconds.")]
	public float delay = 0;


	/// <summary>
	/// Called at the start of the game.
	/// Tries to find the MoCap client singleton and then 
	/// registers this object as a listener with the client.
	/// </summary>
	/// 
	void Start() 
	{
		// try to find the client singleton
		client = FindObjectOfType<MoCapClient>();

		// sanity checks
		if (client != null)
		{
			client.AddActorListener(this);
		}
		else
		{
			Debug.LogWarning("No MoCapClient Component defined in the scene.");
		}

		if ( boneTemplate == null )
		{
			Debug.LogWarning("No Bone template defined");
		}

		skeletonNode = null;
		dataBuffers  = new Dictionary<Bone, MoCapDataBuffer>();

		// let's assume the worst first and check if the actor exists after 1 second
		actorExists = false;
		StartCoroutine(CheckActorName());
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
		foreach ( Bone bone in bones )
		{
			// add empty for position/orientation
			GameObject boneNode = new GameObject();
			boneNode.name = bone.name;

			if (boneTemplate != null)
			{
				// add subnode for visual that can be scaled
				GameObject boneRepresentation = GameObject.Instantiate(boneTemplate);
				boneRepresentation.transform.parent = boneNode.transform;
				boneRepresentation.transform.localScale = bone.length * Vector3.one;
				boneRepresentation.transform.localRotation = new Quaternion();
				boneRepresentation.name = bone.name + "_visual";
			}

			if (bone.parent != null)
			{
				// attach to parent node
				GameObject parentObject = dataBuffers[bone.parent].GetGameObject();
				boneNode.transform.parent = parentObject.transform;
			}
			else
			{
				// no parent > attach to base skeleton node
				boneNode.transform.parent = skeletonNode.transform;
			}
			
			dataBuffers[bone] = new MoCapDataBuffer(boneNode, delay);
		}
	}


	//// <summary>
	/// Called once per frame.
	/// </summary>
	/// 
	void Update() 
	{
		if ((client == null) || (skeletonNode == null))
			return;

		// update bones
		foreach ( KeyValuePair<Bone, MoCapDataBuffer> entry in dataBuffers )
		{
			Bone            bone   = entry.Key;
			MoCapDataBuffer buffer = entry.Value; 
			GameObject      obj    = buffer.GetGameObject();

			// pump bone data through buffer
			MoCapDataBuffer.MoCapData data = buffer.Process(bone);

			// update bone game object
			if (data.tracked)
			{
				obj.transform.localPosition = data.pos;
				obj.transform.localRotation = data.rot;
				obj.SetActive(true);

				if (bone.parent != null)
				{
					Debug.DrawLine(obj.transform.parent.position, obj.transform.position, Color.red);
				}	
			}
			else
			{
				// bone not tracked anymore
				obj.SetActive(false);
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
		// create marker position array if necessary
		// but only when tracking is OK, otherwise the bone lengths are undefined
		if ( (skeletonNode == null) && actor.bones[0].tracked )
		{
			CreateBones(actor.bones);
		}

		// Entering this callback shows that the actor exists
		if ( !actorExists )
		{
			actorExists = true;
			Debug.Log("Skeleton Renderer '" + this.name + "' controlled by MoCap actor '" + actorName + "'.");
		}
	}


	/// <summary>
	/// Coroutine that checks if any MoCap data was received
	/// 1 second after start of the program. If not,
	/// this object unregisters from the client.
	/// </summary>
	/// <returns>The coroutine instance</returns>
	/// 
	IEnumerator CheckActorName()
	{
		// wait 1 second
		yield return new WaitForSeconds(1);
		// check
		if ( !actorExists ) 
		{
			// one second has passed since the beginning of the scene
			// and no MoCap data has been received:
			// -> The actor does not seem to exist
			Debug.LogWarning ("No Mocap data received for actor '" + actorName + "'.");
			client.RemoveActorListener(this);
			client = null;
		}
	}


	private MoCapClient                       client;
	private bool                              actorExists;
	private GameObject                        skeletonNode;
	private Dictionary<Bone, MoCapDataBuffer> dataBuffers;
}
