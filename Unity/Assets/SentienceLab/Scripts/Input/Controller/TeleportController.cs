#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using SentienceLab.Input;
using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Component for an object that can be aimed at for teleporting.
	/// This component does NOT use the event system.
	/// </summary>

	[AddComponentMenu("Locomotion/Teleport Controller")]
	[DisallowMultipleComponent]

	public class TeleportController : MonoBehaviour
	{
		public string    actionName   = "teleport";
		public string    groundTag    = "floor";
		public Transform cameraNode   = null;
		public Transform targetMarker = null;

		public ActivationType activationType = ActivationType.OnTrigger;


		public enum ActivationType
		{
			OnTrigger,
			ActivateAndRelease
		}



		void Start()
		{
			ray             = GetComponentInChildren<PointerRay>();
			transportAction = InputHandler.Find(actionName);

			if (ray == null)
			{
				// activate and release doesn't make much sense without the ray
				activationType  = ActivationType.OnTrigger;
				rayAlwaysActive = false;
			}
			else
			{
				rayAlwaysActive = ray.rayEnabled;
			}

			teleporter = GameObject.FindObjectOfType<Teleporter>();
		}


		void Update()
		{
			if ((teleporter == null) || !teleporter.IsReady()) return;

			bool doTransport = false;
			bool doAim       = false;

			if (activationType == ActivationType.OnTrigger)
			{
				doAim       = true;
				doTransport = transportAction.IsActivated();
			}
			else
			{
				doAim = transportAction.IsActive();

				ray.rayEnabled = (doAim || rayAlwaysActive);

				if (transportAction.IsDeactivated())
				{
					doTransport = true;
				}
			}

			RaycastHit hit;
			if (ray != null)
			{
				hit = ray.GetRayTarget();
			}
			else
			{
				// no ray component > do a basic raycast here
				Ray tempRay = new Ray(transform.position, transform.forward);
				Physics.Raycast(tempRay, out hit);
			}

			if ((hit.distance > 0) && (hit.transform != null) && hit.transform.gameObject.tag.Equals(groundTag))
			{
				if (doTransport && (teleporter != null))
				{
					// here we go: hide marker...
					targetMarker.gameObject.SetActive(false);
					// ...and activate teleport
					teleporter.Activate(cameraNode.transform.position, hit.point);
				}
				else
				{
					if ((targetMarker != null) && doAim)
					{
						targetMarker.gameObject.SetActive(true);
						float yaw = cameraNode.transform.rotation.eulerAngles.y;
						targetMarker.position = hit.point;
						targetMarker.localRotation = Quaternion.Euler(0, yaw, 0);
					}
				}
			}
			else
			{
				if (targetMarker != null)
				{
					targetMarker.gameObject.SetActive(false);
				}
			}
		}


		private PointerRay   ray;
		private bool         rayAlwaysActive;
		private InputHandler transportAction;
		private Teleporter   teleporter;
	}
}
