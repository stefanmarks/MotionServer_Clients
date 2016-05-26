using UnityEngine;
using System.Collections.Generic;

namespace MoCap
{
	/// <summary>
	/// Class for rendering markers for a Motion Capture actor.
	/// </summary>
	///

	[AddComponentMenu("Motion Capture/Marker Renderer")]

	public class MarkerRenderer : MonoBehaviour, SceneListener
	{
		[Tooltip("The name of the MoCap actor to render.")]
		public string actorName;

		[Tooltip("A template game object for how to display the markers.")]
		public GameObject markerTemplate;


		void Start()
		{
			// initialise variables
			markerNode  = null;
			actor       = null;
			dataBuffers = new Dictionary<Marker, MoCapDataBuffer>();

			// sanity checks
			if (markerTemplate == null)
			{
				Debug.LogWarning("No marker template defined");
			}

			// start receiving MoCap data
			MoCapClient.GetInstance().AddSceneListener(this);
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
			if (markerNode == null)
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


		public void SceneUpdated(Scene scene)
		{
			// create marker position array if necessary
			if ((markerNode == null) && (actor !=null))
			{
				CreateMarkers(actor.markers);
			}
		}


		public void SceneChanged(Scene scene)
		{
			// actor has changed > rebuild markers on next update
			if (markerNode != null)
			{
				// if necessary, destroy old container
				GameObject.Destroy(markerNode);
				markerNode = null;
			}

			actor = scene.FindActor(actorName);
			if (actor != null)
			{
				Debug.Log("Marker Renderer '" + this.name + "' controlled by MoCap actor '" + actorName + "'.");
			}
			else
			{
				Debug.LogWarning("Marker Renderer '" + this.name + "' cannot find MoCap actor '" + actorName + "'.");
			}
		}


		private GameObject                          markerNode;
		private Actor                               actor;
		private Dictionary<Marker, MoCapDataBuffer> dataBuffers;
	}

}
