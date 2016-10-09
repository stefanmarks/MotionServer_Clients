using UnityEngine;
using UnityEngine.EventSystems;
using SentienceLab.Input;
using SentienceLab.MoCap;

[AddComponentMenu("Event/Tracked Controller Input Module")]
public class TrackedControllerInputModule : BaseInputModule
{
	[System.Serializable]
	public class Controller
	{
		public Transform  trackedObject;
		public string     actionName;
	}

	[Tooltip("Layers that this tracked controller reacts to")]
	public LayerMask layerMask;

	[Tooltip("Tracked controllers and their action name for clicking")]
	public Controller[] controllers;


	public override bool ShouldActivateModule()
	{
		bool activate = base.ShouldActivateModule();
		if (activate && !activated && (controllers != null))
		{
			// is at least one of the controller objects active?
			bool controllersActive = false;
			foreach (Controller controller in controllers)
			{
				if (controller.trackedObject.gameObject.activeInHierarchy)
				{
					controllersActive = true;
					break;
				}
			}
			// if so, activate this module
			activate = controllersActive;
		}
		return activate;
	}


	public override void ActivateModule()
	{
		base.ActivateModule();

		if (!activated)
		{
			controllerCamera = new GameObject("Controller UI Camera").AddComponent<Camera>();
			controllerCamera.clearFlags      = CameraClearFlags.Depth;
			controllerCamera.cullingMask = layerMask; 
			controllerCamera.depth       = -100;
			controllerCamera.nearClipPlane   = 0.05f;
			controllerCamera.farClipPlane    = 10.0f;
			controllerCamera.stereoTargetEye = StereoTargetEyeMask.None;
			PhysicsRaycaster prc = (PhysicsRaycaster) controllerCamera.gameObject.AddComponent<PhysicsRaycaster>();
			prc.eventMask = layerMask;

			currentPoint    = new GameObject[controllers.Length];
			currentPressed  = new GameObject[controllers.Length];
			currentDragging = new GameObject[controllers.Length];
			pointEvents     = new PointerEventData[controllers.Length];

			Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>();
			foreach (Canvas canvas in canvases)
			{
				canvas.worldCamera = controllerCamera;
			}

			actionHandlers = new InputHandler[controllers.Length];
			for (int idx = 0; idx < controllers.Length; idx++)
			{
				actionHandlers[idx] = InputHandler.Find(controllers[idx].actionName);
			}

			activated = true;
		}
			}
	

	public override void DeactivateModule()
	{
		base.DeactivateModule();

		if (activated)
		{
			currentPoint    = null;
			currentPressed  = null;
			currentDragging = null;
			pointEvents     = null;

			Destroy(controllerCamera);
			controllerCamera = null;

			activated = false;
		}
	}

	// use screen midpoint as locked pointer location, enabling look location to be the "mouse"
	private void GetLookPointerEventData(int index)
	{
		if (pointEvents[index] == null)
			pointEvents[index] = new PointerEventData(base.eventSystem);
		else
			pointEvents[index].Reset();

		pointEvents[index].delta       = Vector2.zero;
		pointEvents[index].position    = new Vector2(Screen.width / 2, Screen.height / 2);
		pointEvents[index].scrollDelta = Vector2.zero;

		base.eventSystem.RaycastAll(pointEvents[index], m_RaycastResultCache);
		pointEvents[index].pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
		m_RaycastResultCache.Clear();
	}

	// update the cursor location and whether it is enabled
	// this code is based on Unity's DragMe.cs code provided in the UI drag and drop example
	private void UpdateCursor(int index, PointerEventData pointData)
	{
		PointerRay ray = controllers[index].trackedObject.GetComponentInChildren<PointerRay>();
		if (ray)
		{
			ray.OverrideRayTarget(Vector3.zero);
		}

		if (pointEvents[index].pointerCurrentRaycast.gameObject != null)
		{
			if (pointData.pointerEnter != null)
			{
				RectTransform draggingPlane = pointData.pointerEnter.GetComponent<RectTransform>();
				Vector3 globalLookPos;
				if ((draggingPlane != null) && RectTransformUtility.ScreenPointToWorldPointInRectangle(draggingPlane, pointData.position, pointData.enterEventCamera, out globalLookPos))
				{
					if (ray)
					{
						ray.OverrideRayTarget(globalLookPos);
					}
//					Cursors[index].position = globalLookPos;
//					Cursors[index].rotation = draggingPlane.rotation;

					// scale cursor based on distance to camera
//					float lookPointDistance = (Cursors[index].position - Camera.main.transform.position).magnitude;
//					float cursorScale = lookPointDistance * NormalCursorScale;
//					if (cursorScale < NormalCursorScale)
//					{
//						cursorScale = NormalCursorScale;
//					}

//					Cursors[index].localScale = Vector3.one * cursorScale;
				}
			}
		}
		else
		{
			//			Cursors[index].gameObject.SetActive(false);
			if (ray)
			{
				ray.OverrideRayTarget(Vector3.zero);
			}
		}
	}

	// clear the current selection
	public void ClearSelection()
	{
		if (base.eventSystem.currentSelectedGameObject)
		{
			base.eventSystem.SetSelectedGameObject(null);
		}
	}

	// select a game object
	private void Select(GameObject go)
	{
		ClearSelection();

		if (ExecuteEvents.GetEventHandler<ISelectHandler>(go))
		{
			base.eventSystem.SetSelectedGameObject(go);
		}
	}

	// send update event to selected object
	// needed for InputField to receive keyboard input
	private bool SendUpdateEventToSelectedObject()
	{
		if (base.eventSystem.currentSelectedGameObject == null)
			return false;

		BaseEventData data = GetBaseEventData();

		ExecuteEvents.Execute(base.eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);

		return data.used;
	}

	private void UpdateCameraPosition(int index)
	{
		controllerCamera.transform.position = controllers[index].trackedObject.position;
		controllerCamera.transform.forward  = controllers[index].trackedObject.forward;
	}


	// Process is called by UI system to process events
	public override void Process()
	{
		// send update events if there is a selected object - this is important for InputField to receive keyboard events
		SendUpdateEventToSelectedObject();

		// see if there is a UI element that is currently being looked at
		for (int index = 0; index < controllers.Length; index++)
		{
			if (controllers[index].trackedObject.gameObject.activeInHierarchy == false)
			{
//				if (Cursors[index].gameObject.activeInHierarchy == true)
				{
//					Cursors[index].gameObject.SetActive(false);
				}
				continue;
			}

			UpdateCameraPosition(index);
			GetLookPointerEventData(index);

			currentPoint[index] = pointEvents[index].pointerCurrentRaycast.gameObject;

			// handle enter and exit events (highlight)
			base.HandlePointerExitAndEnter(pointEvents[index], currentPoint[index]);

			// update cursor
			UpdateCursor(index, pointEvents[index]);

			if (controllers[index] != null)
			{
				if (actionHandlers[index].IsActivated())
				{
					ClearSelection();

					pointEvents[index].pressPosition = pointEvents[index].position;
					pointEvents[index].pointerPressRaycast = pointEvents[index].pointerCurrentRaycast;
					pointEvents[index].pointerPress = null;

					if (currentPoint[index] != null)
					{
						currentPressed[index] = currentPoint[index];

						GameObject newPressed = ExecuteEvents.ExecuteHierarchy(currentPressed[index], pointEvents[index], ExecuteEvents.pointerDownHandler);

						if (newPressed == null)
						{
							// some UI elements might only have click handler and not pointer down handler
							newPressed = ExecuteEvents.ExecuteHierarchy(currentPressed[index], pointEvents[index], ExecuteEvents.pointerClickHandler);
							if (newPressed != null)
							{
								currentPressed[index] = newPressed;
							}
						}
						else
						{
							currentPressed[index] = newPressed;
							// we want to do click on button down at same time, unlike regular mouse processing
							// which does click when mouse goes up over same object it went down on
							// reason to do this is head tracking might be jittery and this makes it easier to click buttons
							ExecuteEvents.Execute(newPressed, pointEvents[index], ExecuteEvents.pointerClickHandler);
						}

						if (newPressed != null)
						{
							pointEvents[index].pointerPress = newPressed;
							currentPressed[index] = newPressed;
							Select(currentPressed[index]);
						}

						ExecuteEvents.Execute(currentPressed[index], pointEvents[index], ExecuteEvents.beginDragHandler);
						pointEvents[index].pointerDrag = currentPressed[index];
						currentDragging[index] = currentPressed[index];
					}
				}

				if (actionHandlers[index].IsDeactivated())
				{
					if (currentDragging[index])
					{
						ExecuteEvents.Execute(currentDragging[index], pointEvents[index], ExecuteEvents.endDragHandler);
						if (currentPoint[index] != null)
						{
							ExecuteEvents.ExecuteHierarchy(currentPoint[index], pointEvents[index], ExecuteEvents.dropHandler);
						}
						pointEvents[index].pointerDrag = null;
						currentDragging[index] = null;
					}
					if (currentPressed[index])
					{
						ExecuteEvents.Execute(currentPressed[index], pointEvents[index], ExecuteEvents.pointerUpHandler);
						pointEvents[index].rawPointerPress = null;
						pointEvents[index].pointerPress = null;
						currentPressed[index] = null;
					}

					ClearSelection();
				}

				// drag handling
				if (currentDragging[index] != null)
				{
					ExecuteEvents.Execute(currentDragging[index], pointEvents[index], ExecuteEvents.dragHandler);
				}
			}
		}
	}


	private bool               activated = false;
	private InputHandler[]    actionHandlers;
	private GameObject[]       currentPoint;
	private GameObject[]       currentPressed;
	private GameObject[]       currentDragging;
	private PointerEventData[] pointEvents;
	private Camera controllerCamera;
}
