using UnityEngine;
using System.Collections.Generic;

namespace MoCap
{
	/// <summary>
	/// Class for rendering markers for a Motion Capture actor.
	/// </summary>
	///

	[AddComponentMenu("Motion Capture/Marker Renderer")]

	public class MarkerRenderer : MonoBehaviour, ActorListener
	{
		[Tooltip("The name of the MoCap actor to render.")]
		public string actorName;

		[Tooltip("A template game object for how to display the markers.")]
		public GameObject markerTemplate;


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
			if (client != null)
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
				Debug.LogWarning("No marker template defined");
			}
		}


		/// <summary>
		/// Called when object is about to be destroyed.
		/// Unregisters as listener from the MoCap client.
		/// </summary>
		/// 
		void OnDestroy()
		{
			if (client != null)
			{
				client.RemoveActorListener(this);
			}
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
			markerNode.transform.parent = this.transform;
			markerNode.transform.localPosition = Vector3.zero;
			markerNode.transform.localRotation = Quaternion.identity;
			markerNode.transform.localScale = Vector3.one;

			if (markerTemplate != null)
			{
				// create copies of the marker template
				foreach (Marker marker in markers)
				{
					GameObject markerRepresentation = GameObject.Instantiate(markerTemplate);
					markerRepresentation.name = marker.name;
					markerRepresentation.transform.parent = markerNode.transform;
					dataBuffers[marker] = new MoCapDataBuffer(marker.name, this.gameObject, markerRepresentation);
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
				Marker marker = entry.Key;
				MoCapDataBuffer buffer = entry.Value;
				GameObject obj = buffer.GameObject;

				// pump marker data through buffer
				MoCapData data = buffer.Process(marker);

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
			if (markerNode == null)
			{
				CreateMarkers(actor.markers);
			}
		}


		public void ActorChanged(Actor actor)
		{
			// actor has changed > rebuild markers on next update
			if (markerNode != null)
			{
				// if necessary, destroy old container
				GameObject.Destroy(markerNode);
				markerNode = null;
			}

			if (actor != null)
			{
				Debug.Log("Marker Renderer '" + this.name + "' controlled by MoCap actor '" + actorName + "'.");
			}
			else
			{
				Debug.LogWarning("Marker Renderer '" + this.name + "' cannot find MoCap actor '" + actorName + "'.");
			}
		}


		private MoCapClient                         client;
		private GameObject                          markerNode;
		private Dictionary<Marker, MoCapDataBuffer> dataBuffers;
	}

}
