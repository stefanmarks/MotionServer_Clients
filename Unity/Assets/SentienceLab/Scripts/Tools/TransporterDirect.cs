using UnityEngine;
using SentienceLab.Input;
using System;

public class TransporterDirect : MonoBehaviour
{
	public string         actionName         = "teleport";
	public string         groundTag          = "floor";
	public Transform      offsetObject       = null;
	public Transform      cameraNode         = null;
	public Transform      targetMarker       = null;
	public ActivationType activationType     = ActivationType.OnTrigger;
	public float          transitionTime     = 0.1f;
	public TransitionType transitionType     = TransitionType.MoveLinear;


	public enum ActivationType
	{
		OnTrigger,
		ActivateAndRelease
	}

	public enum TransitionType
	{
		Blink,
		MoveLinear,
		MoveSmooth
	}


	void Start()
	{
		ray             = GetComponentInChildren<PointerRay>();
		transportAction = InputHandler.Find(actionName);

		if (ray == null)
		{
			// activate and release doesn't make much sense without the ray
			activationType = ActivationType.OnTrigger;
		}

		//state = State.Inactive;
	}


	void Update()
	{
		bool doTransport = false;

		if (activationType == ActivationType.OnTrigger)
		{
			doTransport = transportAction.IsActivated();
		}
		else
		{
			ray.SetEnabled(transportAction.IsActive());
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

		if ( (hit.distance > 0) && (hit.transform.gameObject != null) && hit.transform.gameObject.tag.Equals(groundTag) )
		{
			if (doTransport)
			{
				// calculate target point
				Vector3 startPoint = offsetObject.position;
				Vector3 offset     = hit.point - this.transform.position;
				offset.y = 0;
				Vector3 endPoint = startPoint + offset;
				
				// activate transition
				switch ( transitionType )
				{
					case TransitionType.Blink:
						transition = new Transition_Blink(endPoint, transitionTime, this.gameObject);
						break;

					case TransitionType.MoveLinear:
						transition = new Transition_Move(startPoint, endPoint, transitionTime, false);
						break;

					case TransitionType.MoveSmooth:
						transition = new Transition_Move(startPoint, endPoint, transitionTime, true);
						break;
				}
				ray.SetEnabled(false);
			}
			else
			{
				if (targetMarker != null)
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

		if ( transition != null )
		{
			transition.Update(offsetObject);
			if ( transition.IsFinished() )
			{
				transition.Cleanup();
				transition = null;
				if (activationType == ActivationType.OnTrigger)
				{
					ray.SetEnabled(true);
				}
			}
		}
	}


	public void OnGUI()
	{
		if ( transition != null )
		{
			transition.UpdateUI();
		}
	}


	private enum State
	{
		Inactive,
		Activated,
		Ready,
		Transporting
	}


	private PointerRay    ray;
	private InputHandler transportAction;
	private ITransition   transition;
	//private State         state;


	private interface ITransition
	{
		void Update(Transform offsetObject);
		void UpdateUI();
		bool IsFinished();
		void Cleanup();
	}


	private class Transition_Blink : ITransition
	{
		public Transition_Blink(Vector3 endPoint, float duration, GameObject parent)
		{
			this.endPoint = endPoint;
			this.duration = duration;

			progress = 0;
			moved    = false;

			eyelidParent = new GameObject("Eyelids");

			GameObject goTop          = new GameObject("Top");
			goTop.transform.parent    = eyelidParent.transform;
			eyelidTop                 = goTop.AddComponent<GUITexture>();
			eyelidTop.texture         = Texture2D.whiteTexture;
			eyelidTop.color           = Color.black;
			eyelidTop.pixelInset      = new Rect(0, 0, 0, 0);

			GameObject goBottom       = new GameObject("Bottom");
			goBottom.transform.parent = eyelidParent.transform;
			eyelidBottom              = goBottom.AddComponent<GUITexture>();
			eyelidBottom.texture      = Texture2D.whiteTexture;
			eyelidBottom.color        = Color.black;
			eyelidBottom.pixelInset   = new Rect(0, 0, 0, 0);
		}

		public void Update(Transform offsetObject)
		{
			// move immediately to B when blink is half way ("eyelids" closed)
			progress += Time.deltaTime / duration;
			progress  = Math.Min(1, progress);
			if ( (progress >= 0.5f) && !moved)
			{
				offsetObject.position = endPoint;
				moved = true; // only move once
			}
		}

		public void UpdateUI()
		{
			// draw "eyelids"
			float height = 1 - Math.Abs(progress * 2 - 1); // Vertical lid position from [0....1....0]
			height *= Screen.height / 2;
			eyelidTop.pixelInset    = new Rect(0,                      0, Screen.width, height);
			eyelidBottom.pixelInset = new Rect(0, Screen.height - height, Screen.width, height);
		}

		public bool IsFinished()
		{
			return progress >= 1; // movement has finished
		}

		public void Cleanup()
		{
			GameObject.Destroy(eyelidParent);
		}


		private Vector3    endPoint;
		private float      duration, progress;
		private bool       moved;
		private GameObject eyelidParent;
		private GUITexture eyelidTop, eyelidBottom;
	}


	private class Transition_Move : ITransition
	{
		public Transition_Move(Vector3 startPoint, Vector3 endPoint, float duration, bool smooth)
		{
			this.startPoint = startPoint;
			this.endPoint   = endPoint;
			this.duration   = duration;
			this.smooth     = smooth;

			progress = 0;
		}

		public void Update(Transform offsetObject)
		{
			// move from A to B
			progress += Time.deltaTime / duration;
			progress  = Math.Min(1, progress);
			// linear: lerpFactor = progress. smooth: lerpFactor = sin(progress * PI/2) ^ 2
			float lerpFactor = smooth ? (float) Math.Pow(Math.Sin(progress * Math.PI / 2), 2) : progress;
			offsetObject.position = Vector3.Lerp(startPoint, endPoint, lerpFactor);
		}

		public void UpdateUI()
		{
			// nothing to do
		}

		public bool IsFinished()
		{
			return progress >= 1; // movement has finished
		}

		public void Cleanup()
		{
			// nothing to do
		}

		private Vector3 startPoint, endPoint;
		private float   duration, progress;
		private bool    smooth;
	}
}
