using UnityEngine;
using UnityEngine.EventSystems;
using VR.Input;

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
	public Controller[] Controllers;


	protected override void Start()
	{
		base.Start();

		if (!initialized)
		{
			controllerCamera = new GameObject("Controller UI Camera").AddComponent<Camera>();
			controllerCamera.clearFlags  = CameraClearFlags.Nothing; //CameraClearFlags.Depth;
			controllerCamera.cullingMask = layerMask; 
			controllerCamera.depth       = -100;
			controllerCamera.stereoTargetEye = StereoTargetEyeMask.None;
			controllerCamera.gameObject.AddComponent<PhysicsRaycaster>();

			CurrentPoint    = new GameObject[Controllers.Length];
			CurrentPressed  = new GameObject[Controllers.Length];
			CurrentDragging = new GameObject[Controllers.Length];
			PointEvents     = new PointerEventData[Controllers.Length];

			Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>();
			foreach (Canvas canvas in canvases)
			{
				canvas.worldCamera = controllerCamera;
			}

			actionHandlers = new ActionHandler[Controllers.Length];
			for (int idx = 0; idx < Controllers.Length; idx++)
			{
				actionHandlers[idx] = ActionHandler.Find(Controllers[idx].actionName);
			}
	
			initialized = true;
		}
	}

	// use screen midpoint as locked pointer location, enabling look location to be the "mouse"
	private void GetLookPointerEventData(int index)
	{
		if (PointEvents[index] == null)
			PointEvents[index] = new PointerEventData(base.eventSystem);
		else
			PointEvents[index].Reset();

		PointEvents[index].delta       = Vector2.zero;
		PointEvents[index].position    = new Vector2(Screen.width / 2, Screen.height / 2);
		PointEvents[index].scrollDelta = Vector2.zero;

		base.eventSystem.RaycastAll(PointEvents[index], m_RaycastResultCache);
		PointEvents[index].pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
		m_RaycastResultCache.Clear();
	}

	// update the cursor location and whether it is enabled
	// this code is based on Unity's DragMe.cs code provided in the UI drag and drop example
	private void UpdateCursor(int index, PointerEventData pointData)
	{
		PointerRay ray = Controllers[index].trackedObject.GetComponentInChildren<PointerRay>();

		if (PointEvents[index].pointerCurrentRaycast.gameObject != null)
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
		controllerCamera.transform.position = Controllers[index].trackedObject.position;
		controllerCamera.transform.forward  = Controllers[index].trackedObject.forward;
	}

	// Process is called by UI system to process events
	public override void Process()
	{
		// send update events if there is a selected object - this is important for InputField to receive keyboard events
		SendUpdateEventToSelectedObject();

		// see if there is a UI element that is currently being looked at
		for (int index = 0; index < Controllers.Length; index++)
		{
			if (Controllers[index].trackedObject.gameObject.activeInHierarchy == false)
			{
//				if (Cursors[index].gameObject.activeInHierarchy == true)
				{
//					Cursors[index].gameObject.SetActive(false);
				}
				continue;
			}

			UpdateCameraPosition(index);
			GetLookPointerEventData(index);

			CurrentPoint[index] = PointEvents[index].pointerCurrentRaycast.gameObject;

			// handle enter and exit events (highlight)
			base.HandlePointerExitAndEnter(PointEvents[index], CurrentPoint[index]);

			// update cursor
			UpdateCursor(index, PointEvents[index]);

			if (Controllers[index] != null)
			{
				if (actionHandlers[index].IsActivated())
				{
					ClearSelection();

					PointEvents[index].pressPosition = PointEvents[index].position;
					PointEvents[index].pointerPressRaycast = PointEvents[index].pointerCurrentRaycast;
					PointEvents[index].pointerPress = null;

					if (CurrentPoint[index] != null)
					{
						CurrentPressed[index] = CurrentPoint[index];

						GameObject newPressed = ExecuteEvents.ExecuteHierarchy(CurrentPressed[index], PointEvents[index], ExecuteEvents.pointerDownHandler);

						if (newPressed == null)
						{
							// some UI elements might only have click handler and not pointer down handler
							newPressed = ExecuteEvents.ExecuteHierarchy(CurrentPressed[index], PointEvents[index], ExecuteEvents.pointerClickHandler);
							if (newPressed != null)
							{
								CurrentPressed[index] = newPressed;
							}
						}
						else
						{
							CurrentPressed[index] = newPressed;
							// we want to do click on button down at same time, unlike regular mouse processing
							// which does click when mouse goes up over same object it went down on
							// reason to do this is head tracking might be jittery and this makes it easier to click buttons
							ExecuteEvents.Execute(newPressed, PointEvents[index], ExecuteEvents.pointerClickHandler);
						}

						if (newPressed != null)
						{
							PointEvents[index].pointerPress = newPressed;
							CurrentPressed[index] = newPressed;
							Select(CurrentPressed[index]);
						}

						ExecuteEvents.Execute(CurrentPressed[index], PointEvents[index], ExecuteEvents.beginDragHandler);
						PointEvents[index].pointerDrag = CurrentPressed[index];
						CurrentDragging[index] = CurrentPressed[index];
					}
				}

				if (actionHandlers[index].IsDeactivated())
				{
					if (CurrentDragging[index])
					{
						ExecuteEvents.Execute(CurrentDragging[index], PointEvents[index], ExecuteEvents.endDragHandler);
						if (CurrentPoint[index] != null)
						{
							ExecuteEvents.ExecuteHierarchy(CurrentPoint[index], PointEvents[index], ExecuteEvents.dropHandler);
						}
						PointEvents[index].pointerDrag = null;
						CurrentDragging[index] = null;
					}
					if (CurrentPressed[index])
					{
						ExecuteEvents.Execute(CurrentPressed[index], PointEvents[index], ExecuteEvents.pointerUpHandler);
						PointEvents[index].rawPointerPress = null;
						PointEvents[index].pointerPress = null;
						CurrentPressed[index] = null;
					}

					ClearSelection();
				}

				// drag handling
				if (CurrentDragging[index] != null)
				{
					ExecuteEvents.Execute(CurrentDragging[index], PointEvents[index], ExecuteEvents.dragHandler);
				}
			}
		}
	}


	private ActionHandler[] actionHandlers;

	private GameObject[] CurrentPoint;
	private GameObject[] CurrentPressed;
	private GameObject[] CurrentDragging;

	private PointerEventData[] PointEvents;

	private bool initialized = false;

	private Camera controllerCamera;
}
