using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("Input/Gaze/GUI Reticle")]

public class GuiReticle : MonoBehaviour, IGazePointer
{
	public RectTransform reticleNeutral = null;
	public RectTransform reticleActive = null;
	public Image reticleFuse = null;

	[Tooltip("Minimum distance of the reticle from the camera")]
	public float minimumReticleDistance = 0.5f;

	[Tooltip("Maximum distance of the reticle from the camera")]
	public float maximumReticleDistance = 5.0f;


	void Start()
	{
		reticleDistance      = new Vector3(0, 0, maximumReticleDistance);
		originalReticleScale = transform.localScale;
		reticleScale         = new Vector3(1, 1, 1);
		fuseProgress         = 0;

		reticleNeutral.gameObject.SetActive(false);
		reticleActive.gameObject.SetActive(false);
		reticleFuse.gameObject.SetActive(false);
	}


	void OnEnable()
	{
		GazeInputModule.gazePointer = this;
		
		// create Physics Raycaster if this is attached to a camera
		Camera cam = transform.parent.GetComponent<Camera>();
		if (cam != null)
		{
			PhysicsRaycaster raycaster = cam.GetComponent<PhysicsRaycaster>();
			if (raycaster == null)
			{
				cam.gameObject.AddComponent<PhysicsRaycaster>();
			}
		}
	}


	void OnDisable()
	{
		if (GazeInputModule.gazePointer == (IGazePointer) this)
		{
			GazeInputModule.gazePointer = null;
		}
	}


	void Update()
	{
		transform.localPosition = reticleDistance;
		transform.localScale    = reticleScale;
		if (reticleFuse != null)
		{
			reticleFuse.fillAmount = fuseProgress;
		}
	}


	/// This is called when the 'BaseInputModule' system should be enabled.
	public void OnGazeEnabled()
	{
		// nothing to do here (yet)
	}


	/// This is called when the 'BaseInputModule' system should be disabled.
	public void OnGazeDisabled()
	{
		// nothing to do here (yet)
	}


	/// Called when the user is looking on a valid GameObject. This can be a 3D
	/// or UI element.
	///
	/// The camera is the event camera, the target is the object
	/// the user is looking at, and the intersectionPosition is the intersection
	/// point of the ray sent from the camera on the object.
	public void OnGazeStart(Camera camera, GameObject targetObject, Vector3 intersectionPosition, bool isInteractive)
	{
		SetGazeTarget(camera.transform, intersectionPosition);
		SetReticleState(isInteractive);
		fuseProgress = 0;
	}


	/// Called every frame the user is still looking at a valid GameObject. This
	/// can be a 3D or UI element.
	///
	/// The camera is the event camera, the target is the object the user is
	/// looking at, and the intersectionPosition is the intersection point of the
	/// ray sent from the camera on the object.
	public void OnGazeStay(Camera camera, GameObject targetObject, Vector3 intersectionPosition, float fuseProgress, bool isInteractive)
	{
		SetGazeTarget(camera.transform, intersectionPosition);
		SetReticleState(isInteractive);
		this.fuseProgress = fuseProgress;
	}


	/// Called when the user's look no longer intersects an object previously
	/// intersected with a ray projected from the camera.
	/// This is also called just before **OnGazeDisabled** and may have have any of
	/// the values set as **null**.
	///
	/// The camera is the event camera and the target is the object the user
	/// previously looked at.
	public void OnGazeExit(Camera camera, GameObject targetObject)
	{
		SetGazeDistance(maximumReticleDistance);
		SetReticleState(false);
		fuseProgress = 0;
	}


	/// Called when a trigger event is initiated. This is practically when
	/// the user begins pressing the trigger.
	public void OnGazeTriggerStart(Camera camera)
	{
		// Put your reticle trigger start logic here :)
	}


	/// Called when a trigger event is finished. This is practically when
	/// the user releases the trigger.
	public void OnGazeTriggerEnd(Camera camera)
	{
		// Put your reticle trigger end logic here :)
	}


	public void GetPointerRadius(out float innerRadius, out float outerRadius)
	{
		innerRadius = 0;
		outerRadius = 0.1f;
	}


	public void GetDistanceLimits(out float minimumDistance, out float maximumDistance)
	{
		minimumDistance = minimumReticleDistance;
		maximumDistance = maximumReticleDistance;
	}


	private void SetGazeTarget(Transform cameraTransform, Vector3 target)
	{
		// determine distance to targetpoint
		Vector3 targetLocalPosition = cameraTransform.InverseTransformPoint(target);
		SetGazeDistance(targetLocalPosition.z);
	}


	private void SetGazeDistance(float distance)
	{
		// adapt reticle distance accordingly
		reticleDistance.z = Mathf.Clamp(distance, minimumReticleDistance, maximumReticleDistance);
		// adapt reticle scale accordingly
		reticleScale.x = originalReticleScale.x * reticleDistance.z;
		reticleScale.y = originalReticleScale.y * reticleDistance.z;
	}


	private void SetReticleState(bool interactive)
	{
		reticleNeutral.gameObject.SetActive(!interactive);
		reticleActive.gameObject.SetActive(interactive);
		reticleFuse.gameObject.SetActive(interactive);
	}


	private Vector3          reticleDistance;
	private Vector3          originalReticleScale;
	private Vector3          reticleScale;
	private float            fuseProgress;
}
