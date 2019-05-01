#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using SentienceLab.Input;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SentienceLab
{
	/// <summary>
	/// Input manager that uses motion tracked game objects for interaction.
	/// </summary>
	/// 
	[AddComponentMenu("Event/Tracked Controller Input Module")]
	public class TrackedControllerInputModule : BaseInputModule
	{
		[System.Serializable]
		public class ControllerInfo
		{
			public Transform trackedObject;
			public string    actionName;
		}

		[Tooltip("Layers that this tracked controller reacts to")]
		public LayerMask layerMask;

		[Tooltip("Tracked controllers and their action name for clicking")]
		public ControllerInfo[] controllers;

		[Tooltip("Range of the raycasts for the controllers")]
		public float maxRange = 10;

		[Tooltip("Forcibly enable this module")]
		public bool ForceModuleActive = false;


		public override bool ShouldActivateModule()
		{
			bool activate = base.ShouldActivateModule();
			if (activate && !activated)
			{
				// XR device should be present to have tracked controllers
				activate = UnityEngine.XR.XRDevice.isPresent;
			}
			activate |= ForceModuleActive;
			return activate;
		}


		public override void ActivateModule()
		{
			base.ActivateModule();

			if (!activated)
			{
				// create event camera
				controllerCamera = new GameObject("Controller UI Camera").AddComponent<Camera>();
				controllerCamera.clearFlags      = CameraClearFlags.Depth;
				controllerCamera.cullingMask     = layerMask;
				controllerCamera.depth           = -100;
				controllerCamera.nearClipPlane   = 0.05f;
				controllerCamera.farClipPlane    = maxRange;
				controllerCamera.stereoTargetEye = StereoTargetEyeMask.None;
				PhysicsRaycaster prc = (PhysicsRaycaster) controllerCamera.gameObject.AddComponent<PhysicsRaycaster>();
				prc.eventMask = layerMask;

				// assign event camera to all canvasses
				Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
				foreach (Canvas canvas in canvases)
				{
					if (canvas.renderMode == RenderMode.WorldSpace)
					{
						canvas.worldCamera = controllerCamera;
					}
				}

				// create controller data strctures
				List<ControllerData> controllerDataList = new List<ControllerData>();
				for (int idx = 0; idx < controllers.Length; idx++)
				{
					if (controllers[idx].trackedObject != null)
					{
						ControllerData data = new ControllerData();
						data.transform = controllers[idx].trackedObject.transform;
						data.actionHandler = InputHandler.Find(controllers[idx].actionName);
						data.ray = controllers[idx].trackedObject.GetComponentInChildren<PointerRay>();
						controllerDataList.Add(data);
					}
				}
				controllerData = controllerDataList.ToArray();

				activated = true;
			}
		}


		public override void DeactivateModule()
		{
			base.DeactivateModule();

			if (activated)
			{
				Destroy(controllerCamera);
				controllerCamera = null;

				controllerData = null;
				activated = false;
			}
		}


		// use screen midpoint as locked pointer location, enabling look location to be the "mouse"
		private void GetLookPointerEventData(ref ControllerData info)
		{
			if (info.eventData == null)
			{
				info.eventData = new PointerEventData(base.eventSystem);
			}
			else
			{
				info.eventData.Reset();
			}

			info.eventData.delta       = Vector2.zero;
			info.eventData.position    = new Vector2(Screen.width / 2, Screen.height / 2);
			info.eventData.scrollDelta = Vector2.zero;

			// actually do the raycast
			base.eventSystem.RaycastAll(info.eventData, m_RaycastResultCache);
			info.eventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);

			m_RaycastResultCache.Clear();
		}


		// update the cursor location and whether it is enabled
		// this code is based on Unity's DragMe.cs code provided in the UI drag and drop example
		private void UpdateCursor(ref ControllerData ctrl)
		{
			if (ctrl.ray != null)
			{
				ctrl.ray.OverrideRayTarget(Vector3.zero);
			}

			if (ctrl.eventData.pointerCurrentRaycast.gameObject != null)
			{
				if (ctrl.eventData.pointerEnter != null)
				{
					RectTransform draggingPlane = ctrl.eventData.pointerEnter.GetComponent<RectTransform>();
					Vector3 globalLookPos;
					if ((draggingPlane != null) && 
						RectTransformUtility.ScreenPointToWorldPointInRectangle(
							draggingPlane, 
							ctrl.eventData.position, 
							ctrl.eventData.enterEventCamera, 
							out globalLookPos))
					{
						if (ctrl.ray)
						{
							ctrl.ray.OverrideRayTarget(globalLookPos);
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
				if (ctrl.ray)
				{
					ctrl.ray.OverrideRayTarget(Vector3.zero);
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
			ExecuteEvents.Execute(
				base.eventSystem.currentSelectedGameObject, 
				data, 
				ExecuteEvents.updateSelectedHandler);

			return data.used;
		}


		private void UpdateCameraPosition(ref ControllerData ctrl)
		{
			controllerCamera.transform.position = ctrl.transform.position;
			controllerCamera.transform.rotation = ctrl.transform.rotation;
		}


		// Process is called by UI system to process events
		public override void Process()
		{
			// send update events if there is a selected object - this is important for InputField to receive keyboard events
			SendUpdateEventToSelectedObject();

			// see if there is a UI element that is currently being looked at
			for (int index = 0; index < controllerData.Length; index++)
			{
				ControllerData ctrl = controllerData[index];

				if (ctrl.transform.gameObject.activeInHierarchy == false)
				{
	//				if (Cursors[index].gameObject.activeInHierarchy == true)
					{
	//					Cursors[index].gameObject.SetActive(false);
					}
					continue;
				}

				UpdateCameraPosition(ref ctrl);
				GetLookPointerEventData(ref ctrl);

				// what object are we pointing at?
				ctrl.currentPoint = ctrl.eventData.pointerCurrentRaycast.gameObject;
				// is the pointer ray inactive?
				if ((ctrl.ray != null) && !ctrl.ray.rayEnabled)
				{
					// yes > no active object
					ctrl.currentPoint = null;
				}

				// handle enter and exit events (highlight)
				base.HandlePointerExitAndEnter(ctrl.eventData, ctrl.currentPoint);

				// update cursor
				UpdateCursor(ref ctrl);

				if (ctrl.actionHandler.IsActivated())
				{
					ClearSelection();

					ctrl.eventData.pressPosition = ctrl.eventData.position;
					ctrl.eventData.pointerPressRaycast = ctrl.eventData.pointerCurrentRaycast;
					ctrl.eventData.pointerPress = null;

					if (ctrl.currentPoint != null)
					{
						ctrl.currentPressed = ctrl.currentPoint;

						GameObject newPressed = ExecuteEvents.ExecuteHierarchy(ctrl.currentPressed, ctrl.eventData, ExecuteEvents.pointerDownHandler);

						if (newPressed == null)
						{
							// some UI elements might only have click handler and not pointer down handler
							newPressed = ExecuteEvents.ExecuteHierarchy(ctrl.currentPressed, ctrl.eventData, ExecuteEvents.pointerClickHandler);
							if (newPressed != null)
							{
								ctrl.currentPressed = newPressed;
							}
						}
						else
						{
							ctrl.currentPressed = newPressed;
							// we want to do click on button down at same time, unlike regular mouse processing
							// which does click when mouse goes up over same object it went down on
							// reason to do this is head tracking might be jittery and this makes it easier to click buttons
							ExecuteEvents.Execute(newPressed, ctrl.eventData, ExecuteEvents.pointerClickHandler);
						}

						if (newPressed != null)
						{
							ctrl.eventData.pointerPress = newPressed;
							ctrl.currentPressed = newPressed;
							Select(ctrl.currentPressed);
						}

						ExecuteEvents.Execute(ctrl.currentPressed, ctrl.eventData, ExecuteEvents.beginDragHandler);
						ctrl.eventData.pointerDrag = ctrl.currentPressed;
						ctrl.currentDragging = ctrl.currentPressed;
					}
				}

				if (ctrl.actionHandler.IsDeactivated())
				{
					if (ctrl.currentDragging)
					{
						ExecuteEvents.Execute(ctrl.currentDragging, ctrl.eventData, ExecuteEvents.endDragHandler);
						if (ctrl.currentPoint != null)
						{
							ExecuteEvents.ExecuteHierarchy(ctrl.currentPoint, ctrl.eventData, ExecuteEvents.dropHandler);
						}
						ctrl.eventData.pointerDrag = null;
						ctrl.currentDragging = null;
					}
					if (ctrl.currentPressed)
					{
						ExecuteEvents.Execute(ctrl.currentPressed, ctrl.eventData, ExecuteEvents.pointerUpHandler);
						ctrl.eventData.rawPointerPress = null;
						ctrl.eventData.pointerPress = null;
						ctrl.currentPressed = null;
					}

					ClearSelection();
				}

				// drag handling
				if (ctrl.currentDragging != null)
				{
					ExecuteEvents.Execute(ctrl.currentDragging, ctrl.eventData, ExecuteEvents.dragHandler);
				}
			}
		}


		protected class ControllerData
		{
			public int              index;
			public Transform        transform;
			public InputHandler     actionHandler;
			public GameObject       currentPoint;
			public GameObject       currentPressed;
			public GameObject       currentDragging;
			public PointerEventData eventData;
			public PointerRay       ray;
		}


		private bool               activated = false;
		private ControllerData[]   controllerData;
		private Camera             controllerCamera;
	}
}
