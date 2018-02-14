using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ExternalObserverCamera : MonoBehaviour
{
	public enum DisplayNumber
	{
		Display1, Display2, Display3, Display4, Display5, Display6, Display7, Display8
	}

	[Tooltip("The camera that this observer camera should mirror")]
	public Camera mainCamera = null;

	[Tooltip("Interpolation factor for the external camera movement")]
	public float lerpFactor = 0.1f;

	[Tooltip("Check to avoid roll movement of the external camera")]
	public bool noRoll = true;

	[Tooltip("Which display should the external camera open up")]
	public DisplayNumber displayNumber = DisplayNumber.Display2;


	public void Start()
	{
		// use default camera if not explicitely stated
		if (mainCamera == null)
		{
			mainCamera = Camera.main;
		}

		Display.onDisplaysUpdated += CheckForDisplay;
		CheckForDisplay();
	}
	

	public void Update()
	{
		UpdateObserverCamera(tempLerpFactorOverride > lerpFactor ? tempLerpFactorOverride : lerpFactor * Time.deltaTime);
		tempLerpFactorOverride = 0;
	}


	private void UpdateObserverCamera(float _lerpFactor)
	{
		// adjust position and rotation of observer camera
		Quaternion rotation = noRoll ? Quaternion.LookRotation(mainCamera.transform.forward, Vector3.up) : mainCamera.transform.rotation;
		transform.rotation = Quaternion.Slerp(transform.rotation, rotation, _lerpFactor);
		transform.position = Vector3.Lerp(transform.position, mainCamera.transform.position, _lerpFactor);
	}

	private void CheckForDisplay()
	{
		Camera camera = GetComponent<Camera>();

		int displayIdx = (int)displayNumber;
		if (displayIdx < Display.displays.Length)
		{
			Display d = Display.displays[displayIdx];
			if (!d.active)
			{
				d.Activate();
			}
			Debug.Log("Activated external observer camera on " + displayNumber + " with " + d.systemWidth + "x" + d.systemHeight);

			// set some parameters
			camera.targetDisplay = displayIdx;
			camera.nearClipPlane = mainCamera.nearClipPlane;
			camera.farClipPlane  = mainCamera.farClipPlane;
			camera.stereoTargetEye = StereoTargetEyeMask.None;
			camera.backgroundColor = mainCamera.backgroundColor;

			gameObject.SetActive(true);
			tempLerpFactorOverride = 1; // immediately align
		}
		else
		{
			// no external display > shut down this node
			gameObject.SetActive(false);
			Debug.Log("Could not activate external observer camera on " + displayNumber);
		}
	}

	private float tempLerpFactorOverride = 0; /// Factor for temporarily overriding the global lerp factor
}
