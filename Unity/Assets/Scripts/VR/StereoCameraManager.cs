using UnityEngine;
using UnityEngine.VR;
using System;

/// <summary>
/// Stereo camera manager for applying the necessary adjustments to the template camera
/// for rendering to screens or HMDs or other VR displays.
/// This component requires a child node with a simple camera attached to it that
/// will be duplicated and configured for left/right eye rendering.
/// </summary>
/// 

[AddComponentMenu("VR/Stereo Camera Manager")]

public class StereoCameraManager : MonoBehaviour 
{
	[Tooltip("VR Display Profile to use")] 
	public string DisplayProfileName = "Screen";

	#region Unity Messages
	private void Start()
	{
		if (!Application.isPlaying)
			return;

		// search for the display configuration stated in the parameter
		config = VR.DisplayManager.GetInstance().GetConfig(DisplayProfileName);
		if ( config is VR.ScreenConfig )
		{
			needsCameraConfigure = false;
		}
		else
		{
			needsCameraConfigure = true;
			UpdateCameras();
		}
	}
	
#if !UNITY_ANDROID || UNITY_EDITOR
	private void LateUpdate()
#else
	private void Update()
#endif
	{
		if (!Application.isPlaying)
			return;
		
		UpdateCameras();
	}
	
#endregion



	private void UpdateCameras()
	{
		if ( needsCameraConfigure )
		{
			// Check presence of any VR display which would take care of the configuration automatically
			if (UnityEngine.VR.VRSettings.loadedDevice != VRDeviceType.None )
			{
				Debug.Log("VR Display: " + UnityEngine.VR.VRDevice.model);
				needsCameraConfigure = false;
			}
		}

		if ( needsCameraConfigure )
		{
			// Is there at least one camera component in the children?
			Camera camera = GetComponentInChildren<Camera>();
			if ( camera == null )
			{
				Debug.LogError("No camera component(s) found in node or in children.");
				return;
			}

			// is this a HMD type?
			if (config is VR.HMD_Config)
			{
				VR.HMD_Config hmdConfig = (VR.HMD_Config)config;

				// use that game object as template for left/right eye
				cameraNode = camera.gameObject;
				leftCameraNode = GameObject.Instantiate(cameraNode);
				leftCameraNode.name = "Left Eye Camera";
				leftCameraNode.transform.parent = cameraNode.transform.parent;
				leftCameraNode.transform.localRotation = cameraNode.transform.localRotation;
				leftCameraNode.transform.localScale = Vector3.one;
				ConfigureCamera(leftCameraNode, VRNode.LeftEye, hmdConfig);

				rightCameraNode = GameObject.Instantiate(cameraNode);
				rightCameraNode.name = "Right Eye Camera";
				rightCameraNode.transform.parent = cameraNode.transform.parent;
				rightCameraNode.transform.localRotation = cameraNode.transform.localRotation;
				rightCameraNode.transform.localScale = Vector3.one;
				ConfigureCamera(rightCameraNode, VRNode.RightEye, hmdConfig);

				leftCameraNode.SetActive(true);
				rightCameraNode.SetActive(true);
				cameraNode.SetActive(false);
			}

			needsCameraConfigure = false;
		}
	}


	private void ConfigureCamera(GameObject node, VRNode eye, VR.HMD_Config hmdConfig)
	{
		// adjust camera X-positions based on IPD
		float xDir = 0;
		if      ( eye == VRNode.LeftEye  ) { xDir = -1; }
		else if ( eye == VRNode.RightEye ) { xDir = +1; }

		// shift camera sideways considering any offset orientation
		node.transform.localPosition = (node.transform.localRotation * Vector3.right) * ((hmdConfig.IPD / 2) * xDir);

		foreach ( Camera cam in node.GetComponents<Camera>() ) 
		{
			// Setup perspective projection, with aspect ratio matches viewport
			float top    = (float) Mathf.Tan(Mathf.Deg2Rad * hmdConfig.FieldOfView / 2) * cam.nearClipPlane;
			float bottom = -top;
			float left   = cam.aspect * bottom / 2;
			float right  = -left;

			// apply centre offset and adapt viewport
			float offX = hmdConfig.xOffset * (left - right) / 2;
			if      ( eye == VRNode.LeftEye  ) { cam.rect = new Rect(0.0f, 0, 0.5f, 1);               }
			else if ( eye == VRNode.RightEye ) { cam.rect = new Rect(0.5f, 0, 0.5f, 1); offX = -offX; }
			else                               { cam.rect = new Rect(0.0f, 0, 1.0f, 1); offX = 0;     }

			// calculate off-centre projection matrix
			Matrix4x4 projectionMatrix = cam.projectionMatrix;
			CalculateProjectionMatrix(ref projectionMatrix,  left + offX, right + offX, top, bottom, cam.nearClipPlane, cam.farClipPlane);
			cam.projectionMatrix  = projectionMatrix;

            // add distortion filter
            LensDistortion distortion = cam.GetComponent<LensDistortion>();
            if ( distortion == null )
            {
                distortion = cam.gameObject.AddComponent<LensDistortion>();
                distortion.DistortionShader = Shader.Find("VR/LensDistortion");
            }
            distortion.ApplyConfig(hmdConfig);
            distortion.ScaleIn  = 1.1f;  // TODO: Hardcoded values > calculate automatically
            distortion.ScaleOut = 0.82f; // TODO: Hardcoded values > calculate automatically
        }
	}


	private void CalculateProjectionMatrix(ref Matrix4x4 mtx, float left, float right, float top, float bottom, float near, float far)
	{
		// Frustum matrix:
		//  2*zNear/dx   0          a  0
		//  0            2*zNear/dy b  0
		//  0            0          c  d
		//  0            0         -1  0
		float zNear2 = near + near;
		float dx     = right - left;
		float dy     = top   - bottom;
		float dz     = far   - near;
		float a      =  (right + left)   / dx;
		float b      =  (top   + bottom) / dy;
		float c      = -(far   + near)   / dz;
		float d      = -2 * (far * near) / dz;
		
		mtx[0, 0] = zNear2 / dx;
		mtx[0, 2] = a;
		mtx[1, 1] = zNear2 / dy;
		mtx[1, 2] = b;
		mtx[2, 2] = c;
		mtx[2, 3] = d;
		mtx[3, 2] = -1.0f;
	}

	private VR.DisplayConfig config;

	private GameObject cameraNode;
	private GameObject leftCameraNode;
	private GameObject rightCameraNode;

	private bool needsCameraConfigure;
}
