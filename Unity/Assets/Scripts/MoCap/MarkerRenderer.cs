using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoCap;

/// <summary>
/// Class for rendering markers for a Motion Capture actor.
/// </summary>
///
[AddComponentMenu("Motion Capture/Marker Renderer")]
public class MarkerRenderer : MonoBehaviour, ActorListener, IDelay
{
	[Tooltip("The name of the MoCap actor to link to this renderer.")]
	public string actorName;

	[Tooltip("A template game object for how to display the markers.")]
	public GameObject markerTemplate;

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
		// initialise variables
		markerNode = null;
		dataBuffers = new Dictionary<Marker, MoCapDataBuffer>();

		// try to find the client singleton
		client = FindObjectOfType<MoCapClient>();
		if ( client != null )
		{
			client.AddActorListener(this);
		}
		else
		{
			Debug.LogWarning("No MoCapClient component defined in the scene.");
		}

		// sanity checks
		if (markerTemplate == null)
		{
			Debug.LogWarning("No Marker template defined");
		}

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
		markerNode = new GameObject();
		markerNode.name = "Markers";
		markerNode.transform.parent        = this.transform;
		markerNode.transform.localPosition = Vector3.zero;
		markerNode.transform.localRotation = Quaternion.identity;
		markerNode.transform.localScale    = Vector3.one;

		if (markerTemplate != null)
		{
			// create copies of the marker template
			foreach (Marker marker in markers)
			{
				GameObject markerRepresentation = GameObject.Instantiate(markerTemplate);
				markerRepresentation.name             = marker.name;
				markerRepresentation.transform.parent = markerNode.transform;
				dataBuffers[marker] = new MoCapDataBuffer(markerRepresentation, delay);
			}
		}
	}


	//// <summary>
	/// Called once per frame.
	/// </summary>
	/// 
	void Update() 
	{
		if ((client == null) || (markerNode == null))
			return;

		// update markers
		foreach (KeyValuePair<Marker, MoCapDataBuffer> entry in dataBuffers)
		{
			Marker          marker = entry.Key;
			MoCapDataBuffer buffer = entry.Value;
			GameObject      obj    = buffer.GetGameObject();

			// pump marker data through buffer
			MoCapDataBuffer.MoCapData data = buffer.Process(marker);

			// update marker game object
			if (data.tracked)
			{
				obj.transform.localPosition = data.pos;
				obj.SetActive(true);
			}
			else
			{
				// marker has vanished
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


	public void ActorUpdated(Actor actor)
	{
		// create marker position array if necessary
		if ( (markerTemplate != null) && (markerNode == null) )
		{
			CreateMarkers(actor.markers);
		}

		// Entering this callback shows that the actor exists
		if ( !actorExists )
		{
			actorExists = true;
			Debug.Log("Marker Renderer '" + this.name + "' controlled by MoCap actor '" + actorName + "'.");
		}
	}


	public void ActorChanged(Actor actor)
	{
	}


	public float GetDelay()
	{
		return delay;
	}


	public void SetDelay(float value)
	{
		delay = Mathf.Max(0, value);
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


	private MoCapClient                         client;
	private bool                                actorExists;
	private GameObject                          markerNode;
	private Dictionary<Marker, MoCapDataBuffer> dataBuffers;
}
