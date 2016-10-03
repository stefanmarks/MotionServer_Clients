using UnityEngine;
using UnityEngine.EventSystems;


public class TransporterEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	public Transform offsetObject;

	public Transform groundMarker;


	void Start ()
	{
		raycaster = null;
	}

	
	void Update()
	{
		if (raycaster != null)
		{
			// If this object is still "hit" by the raycast source, update ground marker position and orientation
			RaycastHit hit;
			if (Physics.Raycast(raycaster.transform.position, raycaster.transform.forward, out hit))
			{
				if ((hit.transform == this.transform) || (hit.transform.parent == this.transform))
				{
					float yaw = raycaster.transform.rotation.eulerAngles.y;
					groundMarker.position      = hit.point;
					groundMarker.localRotation = Quaternion.Euler(0, yaw, 0);
				}
			}
		}
	}


	public void OnPointerClick(PointerEventData eventData)
	{
		offsetObject.position = eventData.pointerPressRaycast.worldPosition;
	}


	public void OnPointerEnter(PointerEventData eventData)
	{
		raycaster = eventData.enterEventCamera.transform;
		groundMarker.gameObject.SetActive(true);
	}


	public void OnPointerExit(PointerEventData eventData)
	{
		raycaster = null;
		groundMarker.gameObject.SetActive(false);
	}


	private Transform raycaster;
}
