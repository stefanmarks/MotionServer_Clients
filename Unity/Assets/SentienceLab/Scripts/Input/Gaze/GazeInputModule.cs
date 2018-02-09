// The MIT License (MIT)
//
// Copyright (c) 2015, Unity Technologies & Google, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
//   The above copyright notice and this permission notice shall be included in
//   all copies or substantial portions of the Software.
//
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//   THE SOFTWARE.

using SentienceLab.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;

/// This script provides an implemention of Unity's `BaseInputModule` class, so
/// that Canvas-based (_uGUI_) UI elements can be selected by looking at them and
/// pulling the viewer's trigger or touching the screen.
/// This uses the player's gaze and the trigger as a raycast generator.
///
/// To use, attach to the scene's **EventSystem** object.  Be sure to move it above the
/// other modules, such as _TouchInputModule_ and _StandaloneInputModule_, in order
/// for the user's gaze to take priority in the event system.
///
/// Next, set the **Canvas** object's _Render Mode_ to **World Space**, and set its _Event Camera_
/// to a (mono) camera that is controlled by a GvrHead.  If you'd like gaze to work
/// with 3D scene objects, add a _PhysicsRaycaster_ to the gazing camera, and add a
/// component that implements one of the _Event_ interfaces (_EventTrigger_ will work nicely).
/// The objects must have colliders too.
///
/// GazeInputModule emits the following events: _Enter_, _Exit_, _Down_, _Up_, _Click_, _Select_,
/// _Deselect_, and _UpdateSelected_.  Scroll, move, and submit/cancel events are not emitted.

[AddComponentMenu("Input/Gaze/GazeInputModule")]

public class GazeInputModule : BaseInputModule
{
	/// Determines whether gaze input is active in VR Mode only (`true`), or all of the
	/// time (`false`).  Set to false if you plan to use direct screen taps or other
	/// input when not in VR Mode.
	[Tooltip("Whether gaze input is active in VR Mode only (true), or all the time (false).")]
	public bool vrModeOnly = false;

	[Tooltip("Gaze time after which to trigger an object (0: No fuse mode)")]
	public float defaultFuseTime = 0;

	[Tooltip("Action for triggering the object gazed at")]
	public string triggerActionName = "trigger";

	/// Time in seconds between the pointer down and up events sent by a trigger.
	/// Allows time for the UI elements to make their state transitions.
	[HideInInspector]
	public float clickTime = 0.1f;  // Based on default time for a button to animate to Pressed.

	/// The IGazePointer which will be responding to gaze events.
	public static IGazePointer gazePointer;

	// Active state
	private bool isActive = false;


	public override bool ShouldActivateModule()
	{
		bool activeState = base.ShouldActivateModule();

		activeState = activeState && (XRDevice.isPresent || !vrModeOnly);

		if (activeState != isActive)
		{
			isActive = activeState;

			// Activate gaze pointer
			if (gazePointer != null)
			{
				if (isActive)
				{
					gazePointer.OnGazeEnabled();
				}
			}

			// Calculate hotspot for raycasting (center of camera canvas)
			// Careful with the size of the "canvas" in VR mode
			pointerPos = new Vector2(
					(XRSettings.eyeTextureWidth  > 0 ? XRSettings.eyeTextureWidth  : Screen.width ) / 2,
					(XRSettings.eyeTextureHeight > 0 ? XRSettings.eyeTextureHeight : Screen.height) / 2
				);
		}

		eventCamera = null;
		triggerAction = null;

		return activeState;
	}


	public override void DeactivateModule()
	{
		DisableGazePointer();
		base.DeactivateModule();
		if (pointerData != null)
		{
			HandlePendingClick();
			HandlePointerExitAndEnter(pointerData, null);
			pointerData = null;
		}
		eventSystem.SetSelectedGameObject(null, GetBaseEventData());
	}


	public override bool IsPointerOverGameObject(int pointerId)
	{
		return pointerData != null && pointerData.pointerEnter != null;
	}


	public override void Process()
	{
		if (triggerAction == null)
		{
			triggerAction = InputHandler.Find(triggerActionName);
		}

		// Save the previous Game Object
		GameObject gazeObjectPrevious = GetCurrentGameObject();

		CastRayFromGaze();
		UpdateCurrentObject();
		UpdateReticle(gazeObjectPrevious);

		// Handle input
		if (!triggerAction.IsActivated() && triggerAction.IsActive())
		{
			HandleDrag();
		}
		else if (Time.unscaledTime - pointerData.clickTime < clickTime)
		{
			// Delay new events until clickTime has passed.
		}
		else if (!pointerData.eligibleForClick &&
		          (triggerAction.IsActivated() || (fuseState == FuseState.Trigger)) )
		{
			// New trigger action.
			HandleTrigger();
			fuseState = FuseState.Triggered;
		}
		else if (!triggerAction.IsActive())
		{
			// Check if there is a pending click to handle.
			HandlePendingClick();
		}
	}


	private void CastRayFromGaze()
	{
		if (gazePointer == null) return;

		Vector2 headPose = (eventCamera == null) ? Vector2.zero : NormalizedCartesianToSpherical(eventCamera.transform.forward);

		if (pointerData == null)
		{
			pointerData  = new PointerEventData(eventSystem);
			lastHeadPose = headPose;
		}

		// Cast a ray into the scene
		pointerData.Reset();
		pointerData.position = pointerPos;
		eventSystem.RaycastAll(pointerData, m_RaycastResultCache);

		// discard every hit too far away or too close
		for (int idx = m_RaycastResultCache.Count - 1 ; idx >= 0; idx--)
		{
			RaycastResult r = m_RaycastResultCache[idx];

			// what are the limits of the gaze pointer
			float min, max;
			gazePointer.GetDistanceLimits(out min, out max);

			// consider gaze behaviour modifiers
			GazeBehaviourModifier gbm = (r.gameObject != null) ? r.gameObject.GetComponent<GazeBehaviourModifier>() : null;
			min = (gbm != null) && (gbm.minimumGazeRangeOverride > 0) ? gbm.minimumGazeRangeOverride : min;
			max = (gbm != null) && (gbm.maximumGazeRangeOverride > 0) ? gbm.maximumGazeRangeOverride : max;

			if ((r.distance > max) || (r.distance < min))
			{
				m_RaycastResultCache.RemoveAt(idx);
			}
		}

		pointerData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
		m_RaycastResultCache.Clear();
		pointerData.delta = headPose - lastHeadPose;
		lastHeadPose = headPose;
	}


	private void UpdateCurrentObject()
	{
		// Send enter events and update the highlight.
		var go = pointerData.pointerCurrentRaycast.gameObject;
		HandlePointerExitAndEnter(pointerData, go);
		// Update the current selection, or clear if it is no longer the current object.
		var selected = ExecuteEvents.GetEventHandler<ISelectHandler>(go);
		if (selected == eventSystem.currentSelectedGameObject)
		{
			ExecuteEvents.Execute(
				eventSystem.currentSelectedGameObject,
				GetBaseEventData(),
				ExecuteEvents.updateSelectedHandler);
		}
		else
		{
			eventSystem.SetSelectedGameObject(null, pointerData);
		}
	}


	void UpdateReticle(GameObject previousGazedObject)
	{
		if (gazePointer == null) return;

		eventCamera = pointerData.enterEventCamera ?? eventCamera; // Get the camera

		GameObject gazeObject = GetCurrentGameObject(); // Get the gaze target
		Vector3 intersectionPosition = GetIntersectionPosition();
		bool isInteractive = (pointerData.pointerPress != null) ||
		                     ExecuteEvents.GetEventHandler<IPointerClickHandler>(gazeObject) != null;

		if (gazeObject == previousGazedObject)
		{
			if (gazeObject != null)
			{
				float progress = 0;
				if (isInteractive && fuseState != FuseState.Triggered)
				{ 
					float delta = Time.unscaledTime - gazeStartTime;
					progress = Mathf.Clamp01(delta / fuseTime);
					if ((fuseState == FuseState.Arming) && (delta >= fuseTime))
					{
						fuseState = FuseState.Trigger;
					}
				}
				gazePointer.OnGazeStay(eventCamera, gazeObject, intersectionPosition, progress, isInteractive);
			}
		}
		else
		{
			if (previousGazedObject != null)
			{
				gazePointer.OnGazeExit(eventCamera, previousGazedObject);
				gazeStartTime = 0;
				fuseState     = FuseState.Idle;
			}
			if (gazeObject != null)
			{
				gazePointer.OnGazeStart(eventCamera, gazeObject, intersectionPosition, isInteractive);
				gazeStartTime = Time.unscaledTime;
				GazeBehaviourModifier gbm = gazeObject.GetComponent<GazeBehaviourModifier>();
				fuseTime  = (gbm != null) && (gbm.fuseTimeOverride > 0) ? gbm.fuseTimeOverride : defaultFuseTime;
				fuseState = FuseState.Arming;
			}
		}
	}


	private void HandleDrag()
	{
		bool moving = pointerData.IsPointerMoving();

		if (moving && pointerData.pointerDrag != null && !pointerData.dragging)
		{
			ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.beginDragHandler);
			pointerData.dragging = true;
		}
		else if ( moving )
		{
			ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.moveHandler);
		}

		// Drag notification
		if (pointerData.dragging && moving && pointerData.pointerDrag != null)
		{
			// Before doing drag we should cancel any pointer down state
			// And clear selection!
			if (pointerData.pointerPress != pointerData.pointerDrag)
			{
				ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerUpHandler);

				pointerData.eligibleForClick = false;
				pointerData.pointerPress     = null;
				pointerData.rawPointerPress  = null;
			}
			ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.dragHandler);
		}
	}


	private void HandlePendingClick()
	{
		if (!pointerData.eligibleForClick && !pointerData.dragging) return;

		if (gazePointer != null)
		{
			Camera camera = pointerData.enterEventCamera;
			gazePointer.OnGazeTriggerEnd(camera);
		}

		GameObject go = pointerData.pointerCurrentRaycast.gameObject;

		// Send pointer up and click events.
		ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerUpHandler);
		if (pointerData.eligibleForClick)
		{
			ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerClickHandler);
		}
		else if (pointerData.dragging)
		{
			ExecuteEvents.ExecuteHierarchy(go, pointerData, ExecuteEvents.dropHandler);
			ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.endDragHandler);
		}

		// Clear the click state.
		pointerData.pointerPress = null;
		pointerData.rawPointerPress = null;
		pointerData.eligibleForClick = false;
		pointerData.clickCount = 0;
		pointerData.clickTime = 0;
		pointerData.pointerDrag = null;
		pointerData.dragging = false;
	}


	private void HandleTrigger()
	{
		GameObject go = pointerData.pointerCurrentRaycast.gameObject;

		// Send pointer down event.
		pointerData.pressPosition       = pointerData.position;
		pointerData.pointerPressRaycast = pointerData.pointerCurrentRaycast;
		pointerData.pointerPress =
				ExecuteEvents.ExecuteHierarchy(go, pointerData, ExecuteEvents.pointerDownHandler)
				?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(go);

		// Save the drag handler as well
		pointerData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(go);
		if (pointerData.pointerDrag != null)
		{
			ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.initializePotentialDrag);
		}

		// Save the pending click state.
		pointerData.rawPointerPress  = go;
		pointerData.eligibleForClick = true;
		pointerData.delta            = Vector2.zero;
		pointerData.dragging         = false;
		pointerData.useDragThreshold = true;
		pointerData.clickCount       = 1;
		pointerData.clickTime        = Time.unscaledTime;

		if (gazePointer != null)
		{
			gazePointer.OnGazeTriggerStart(pointerData.enterEventCamera);
		}
	}


	private Vector2 NormalizedCartesianToSpherical(Vector3 cartCoords)
	{
		cartCoords.Normalize();
		if (cartCoords.x == 0) cartCoords.x = Mathf.Epsilon;

		float outPolar = Mathf.Atan(cartCoords.z / cartCoords.x);
		if (cartCoords.x < 0) outPolar += Mathf.PI;

		float outElevation = Mathf.Asin(cartCoords.y);
		return new Vector2(outPolar, outElevation);
	}


	public PointerEventData GetPointerData()
	{
		return pointerData;
	}


	GameObject GetCurrentGameObject()
	{
		if (pointerData != null && pointerData.enterEventCamera != null)
		{
			return pointerData.pointerCurrentRaycast.gameObject;
		}
		return null;
	}


	Vector3 GetIntersectionPosition()
	{
		// Check for camera
		Camera cam = pointerData.enterEventCamera;
		if (cam == null)
		{
			return Vector3.zero;
		}

		float intersectionDistance = pointerData.pointerCurrentRaycast.distance + cam.nearClipPlane;
		Vector3 intersectionPosition = cam.transform.position + cam.transform.forward * intersectionDistance;

		return intersectionPosition;
	}


	void DisableGazePointer()
	{
		if (gazePointer == null) return;

		GameObject currentGameObject = GetCurrentGameObject();
		if (currentGameObject)
		{
			eventCamera = pointerData.enterEventCamera ?? eventCamera;
			gazePointer.OnGazeExit(eventCamera, currentGameObject);
		}

		gazePointer.OnGazeDisabled();
	}


	private enum FuseState
	{
		Idle, Arming, Trigger, Triggered
	}


	private InputHandler     triggerAction;
	private PointerEventData pointerData;
	private Vector2          lastHeadPose, pointerPos;
	private float            gazeStartTime, fuseTime;
	private FuseState        fuseState;
	private Camera           eventCamera;
}
