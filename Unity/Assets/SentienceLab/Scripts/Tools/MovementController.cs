using UnityEngine;
using SentienceLab.Input;

/// <summary>
/// Script to move an object forwards/sideways
/// </summary>
public class MovementController : MonoBehaviour 
{
	public string actionMoveX         = "moveX";
	public string actionMoveY         = "moveY";
	public string actionMoveZ         = "moveZ";
	public float  maxTranslationSpeed =  1.0f;

	public string actionRotateX       = "rotateX";
	public string actionRotateY       = "rotateY";
	public float  maxRotationSpeed    = 90.0f;

	public Transform rotationBasisNode;


	void Start()
	{
		handlerMoveX   = (actionMoveX.Length == 0)   ? null : InputHandler.Find(actionMoveX);
		handlerMoveY   = (actionMoveY.Length == 0)   ? null : InputHandler.Find(actionMoveY);
		handlerMoveZ   = (actionMoveZ.Length == 0)   ? null : InputHandler.Find(actionMoveZ);
		handlerRotateX = (actionRotateX.Length == 0) ? null : InputHandler.Find(actionRotateX);
		handlerRotateY = (actionRotateY.Length == 0) ? null : InputHandler.Find(actionRotateY);
		vec = new Vector3();

		if (rotationBasisNode == null)
		{
			rotationBasisNode = this.transform;
		}
	}


	void Update() 
	{
		if (handlerRotateX != null)
		{
			// rotate up/down (always absolute around X axis)
			transform.RotateAround(rotationBasisNode.position, rotationBasisNode.right, handlerRotateX.GetValue() * maxRotationSpeed * Time.deltaTime);
		}

		if (handlerRotateY != null)
		{
			// rotate left/right (always absolute around Y axis)
			transform.RotateAround(rotationBasisNode.position, Vector3.up, handlerRotateY.GetValue() * maxRotationSpeed * Time.deltaTime);
		}

		if (handlerMoveZ != null)
		{
			// calculate level forward (Z) direction of camera
			vec = rotationBasisNode.forward; vec.y = 0; vec.Normalize();
			// translate forward
			transform.Translate(vec * handlerMoveZ.GetValue() * maxTranslationSpeed * Time.deltaTime, Space.World);
		}

		if (handlerMoveY != null)
		{
			// calculate upwards (Y) direction of camera
			vec = rotationBasisNode.up; vec.Normalize();
			// translate upwards
			transform.Translate(vec * handlerMoveY.GetValue() * maxTranslationSpeed * Time.deltaTime, Space.World);
		}

		if (handlerMoveX != null)
		{
			// calculate level sideways (X) direction of camera
			vec = rotationBasisNode.right; vec.y = 0; vec.Normalize();
			// translate forward
			transform.Translate(vec * handlerMoveX.GetValue() * maxTranslationSpeed * Time.deltaTime, Space.World);
		}
	}

	private InputHandler handlerMoveX, handlerMoveY, handlerMoveZ;  // input handlers for moving
	private InputHandler handlerRotateX, handlerRotateY;            // input handlers for rotating
	private Vector3      vec;
}
