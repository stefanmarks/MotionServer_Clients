using UnityEngine;
using System.Collections;
using MoCap;

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
	public TrackingLostBehaviour trackingLostBehaviour = TrackingLostBehaviour.Zero;


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
		Disable,

		/// <summary>
		/// Set the position/orientation to zero
		/// </summary>
		Zero
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

		position = new Vector3();    position += this.transform.localPosition;
		rotation = new Quaternion(); rotation *= this.transform.localRotation;
		prevTracked = true;

		// let's assume the worst first and check if the actor exists after 1 second
		actorExists = false;
		bone        = null;
		StartCoroutine(CheckActorName());
	}


	//// <summary>
	/// Called once per frame.
	/// </summary>
	/// 
	void Update()
	{
		if (client == null || bone == null)
			return;

		if ( bone.parent != null )
		{

		}

		// update transform (from MoCap data)
		if ( trackingUsage == TrackingUsage.RotationOnly ||
		     trackingUsage == TrackingUsage.PositionAndRotation )
		{
			this.transform.localRotation = rotation;
		}
		if ( trackingUsage == TrackingUsage.PositionOnly ||
		     trackingUsage == TrackingUsage.PositionAndRotation )
		{
			this.transform.localPosition = position;
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
		if (bone == null)
		{
			if (boneName.Length > 0)
			{
				bone = actor.FindBone(boneName);
			}
			else
			{
				// no bone name given > use root bone
				bone = actor.bones[0];
			}

			if (!actorExists && (bone != null))
			{
				if (actorName == bone.name)
				{
					Debug.Log("Unity object '" + this.name + "' controlled by MoCap actor '" + actorName + "'.");
				}
				else
				{
					Debug.Log("Unity object '" + this.name + "' controlled by MoCap actor/bone '" + actorName + "/" + bone.name + "'.");
				}
			}
		}

		if (bone != null)
		{
			if (bone.tracked)
			{
				// when tracking, copy position/rotation
				this.gameObject.SetActive(true);
				position.Set(bone.px, bone.py, bone.pz);
				rotation.Set(bone.qx, bone.qy, bone.qz, bone.qw);
			}
			else if (bone.tracked != prevTracked)
			{
				handleTrackingLost();
			}

			prevTracked = bone.tracked;
		}

		// Entering this callback shows that the actor exists
		actorExists = true;
	}


	/// <summary>
	/// Called when tracking is lost (or never existed).
	/// </summary>
	/// 
	private void handleTrackingLost()
	{
		// tracking state has changed
		switch ( trackingLostBehaviour )
		{
			case TrackingLostBehaviour.Freeze : 
			{
				// don't change pos/rot, so nothing to do here
				break;
			}
				
			case TrackingLostBehaviour.Disable : 
			{
				// enable/disable object
				this.gameObject.SetActive(false);
				break;
			}
				
			case TrackingLostBehaviour.Zero : 
			{
				// zero pos/rot
				position = Vector3.zero;
				rotation = Quaternion.identity;
				break;
			}
		}
	}


	public void ActorChanged(Actor actor)
	{
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
			Debug.LogWarning ("No Mocap data received for actor " + actorName + ".");
			handleTrackingLost();
			client.RemoveActorListener(this);
			client = null;
		}
		else if ( bone == null )
		{
			// no bone has been identified:
			Debug.LogWarning("Bone " + boneName + " not found in actor " + actorName + ".");
			handleTrackingLost();
			client.RemoveActorListener(this);
			client = null;
		}
	}


	private MoCapClient client;
	private bool        actorExists;
	private Bone        bone;
	private Vector3     position;
	private Quaternion  rotation;
	private bool        prevTracked;

}
