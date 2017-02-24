#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.EventSystems;

namespace SentienceLab
{
	/// <summary>
	/// Component for an object that can be aimed at for teleporting.
	/// This component uses the event system.
	/// </summary>

	[AddComponentMenu("Locomotion/Teleport Target")]
	[DisallowMultipleComponent]

	public class TeleportTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
	{
		public Transform groundMarker;


		void Start()
		{
			raycaster     = null;
			raycastResult = new RaycastResult();
			teleporter    = GameObject.FindObjectOfType<Teleporter>();
		}


		void Update()
		{
			if (raycaster != null)
			{
				groundMarker.gameObject.SetActive(teleporter.IsReady());

				// If this object is still "hit" by the raycast source, update ground marker position and orientation
				raycastResult.Clear();
				BaseInputModule bim = EventSystem.current.currentInputModule;
				if (bim is GazeInputModule)
				{
					raycastResult = ((GazeInputModule)bim).GetPointerData().pointerCurrentRaycast;
				}

				if (raycastResult.gameObject != null)
				{
					Transform hit = raycastResult.gameObject.transform;
					if ((hit.transform == this.transform) || (hit.parent == this.transform))
					{
						float yaw = raycaster.transform.rotation.eulerAngles.y;
						groundMarker.position = raycastResult.worldPosition;
						groundMarker.localRotation = Quaternion.Euler(0, yaw, 0);
					}
				}
			}
			else
			{
				groundMarker.gameObject.SetActive(false);
			}
		}


		public void OnPointerClick(PointerEventData eventData)
		{
			if (teleporter != null)
			{
				groundMarker.gameObject.SetActive(false);
				teleporter.Activate(
					Camera.main.transform.position,
					eventData.pointerPressRaycast.worldPosition);
			}
		}


		public void OnPointerEnter(PointerEventData eventData)
		{
			raycaster = eventData.enterEventCamera.transform;
		}


		public void OnPointerExit(PointerEventData eventData)
		{
			raycaster = null;
		}


		private Transform     raycaster;
		private RaycastResult raycastResult;
		private Teleporter    teleporter;
	}
}
