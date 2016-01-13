using UnityEngine;
using System.Collections;
using MoCap;

/// <summary>
/// Class for rendering markers for a Motion Capture actor.
/// </summary>
///
[DisallowMultipleComponent]
[AddComponentMenu("Motion Capture/Marker Renderer")]
public class MarkerRenderer : MonoBehaviour, ActorListener
{
	[Tooltip("The name of the MoCap actor to link to this renderer.")]
	public string actorName;

	[Tooltip("A template game object for how to display the markers.")]
	public GameObject markerTemplate;


	/// <summary>
	/// Called at the start of the game.
	/// Tries to find the MoCap client singleton and then 
	/// registers this object as a listener with the client.
	/// </summary>
	/// 
	void Start () 
	{
		// try to find the client singleton
		client = FindObjectOfType<MoCapClient>();

		if ( client != null )
		{
			if ( client.AddActorListener(this) )
			{
				Debug.Log("Marker Renderer " + this.name + " registered with MoCap client.");
			}
			else
			{
				Debug.LogWarning ("Could not register MoCap actor listener for Marker Renderer " + this.name + ".");
			}
		}
		else
		{
			Debug.LogWarning("No MoCap client defined anywhere in the scene.");
		}

		markerParent    = null;
		markerObjects   = null;
		markerPositions = null;

		// let's assume the worst first and check if the actor exists after 1 second
		actorExists = false;
		StartCoroutine(CheckActorName());
	}


	/// <summary>
	/// Creates copies of the marker template for all markers.
	/// </summary>
	/// <param name="markers">marker data from the MoCap system</param>
	/// 
	private void CreateMarkers(Marker[] markers)
	{
		// create node for containing all the marker objects
		markerParent = new GameObject();
		markerParent.name = "Markers";
		markerParent.transform.parent        = this.transform;
		markerParent.transform.localPosition = Vector3.zero;
		markerParent.transform.localRotation = Quaternion.identity;
		markerParent.transform.localScale    = Vector3.one;

		// create copies of the marker template
		markerObjects   = new GameObject[markers.Length];
		markerPositions = new Vector3[markers.Length];
		for ( int i = 0 ; i < markers.Length ; i++ )
		{
			markerObjects[i] = (GameObject) GameObject.Instantiate(markerTemplate);
			markerObjects[i].name = markers[i].name;
			markerObjects[i].transform.parent = markerParent.transform;
		}

		// make sure the template is not used
		markerTemplate.SetActive(false);
	}


	//// <summary>
	/// Called once per frame.
	/// </summary>
	/// 
	void Update() 
	{
		if (client == null)
			return;

		// update markers
		if ( (markerPositions != null) && (markerObjects != null) )
		{
			for ( int i = 0 ; i < markerPositions.Length ; i++ )
			{
				if ( markerPositions[i].magnitude > 0 )
				{
					markerObjects[i].transform.localPosition = markerPositions[i];
					markerObjects[i].SetActive(true);
				}
				else
				{
					// marker has vanished
					markerObjects[i].SetActive(false);
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
	/// <param name="actor">the actor that has changed</param>
	/// 
	public void ActorChanged(Actor actor)
	{
		// create marker position array if necessary
		if ( (markerTemplate != null) && (markerParent == null) )
		{
			CreateMarkers(actor.markers);
		}

		// copy marker positions if necessary
		if ( markerPositions != null )
		{
			for ( int i = 0 ; i < actor.markers.Length ; i++ )
			{
				Marker m = actor.markers[i];
				markerPositions[i].Set(m.px, m.py, m.pz);
			}
		}

		// Entering this callback shows that the actor exists
		if ( !actorExists )
		{
			actorExists = true;
			Debug.Log("Marker Renderer " + this.name + " controlled by MoCap actor " + actorName + ".");
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
			Debug.LogWarning ("No Mocap data received for actor " + actorName + ".");
			client.RemoveActorListener(this);
			client = null;
		}
	}


	private MoCapClient  client;

	private GameObject   markerParent;
	private GameObject[] markerObjects;
	private Vector3[]    markerPositions;

	private bool         actorExists;
}
