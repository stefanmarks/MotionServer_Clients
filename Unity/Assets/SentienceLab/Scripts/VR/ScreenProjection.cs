using UnityEngine;

namespace VR
{
	/// <summary>
	/// This script should be attached to a Camera object in Unity. 
	/// Once a Plane object is specified as the "projectionScreen", 
	/// the script computes a suitable view and projection matrix for the camera.
	/// The code is based on Robert Kooima's publication "Generalized Perspective Projection," 2009, 
	/// http://csc.lsu.edu/~kooima/pdfs/gen-perspective.pdf 
	/// </summary>
	///

	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("VR/Screen Projection")]

	public class ScreenProjection : MonoBehaviour
	{
		[Tooltip("Name of the Game object that is used to calculate the size of the physical projection screen")]
		public Transform ProjectionScreen = null;


		public void Start()
		{
			if (ProjectionScreen == null)
			{
				//projectionScreen.SetActive(false);
			}

			// retrieve the camera this script is attached to
			camera = GetComponent<Camera>();
		}


		public void LateUpdate()
		{
			if (ProjectionScreen == null) return;

			// calculate corner vertices
			Vector3 pa = ProjectionScreen.transform.TransformPoint(new Vector3(-0.5f, -0.5f, 0)); // lower left
			Vector3 pb = ProjectionScreen.transform.TransformPoint(new Vector3(+0.5f, -0.5f, 0)); // lower right
			Vector3 pc = ProjectionScreen.transform.TransformPoint(new Vector3(-0.5f, +0.5f, 0)); // upper left

			// eye position
			Vector3 pe = transform.position;
			float n = camera.nearClipPlane;
			float f = camera.farClipPlane;

			// some intermediate claculations
			Vector3 va = pa - pe; // from pe to pa
			Vector3 vb = pb - pe; // from pe to pb
			Vector3 vc = pc - pe; // from pe to pc
			Vector3 vr = (pb - pa).normalized; // right axis of screen (pa to pb)
			Vector3 vu = (pc - pa).normalized; // up axis of screen (pa to pc)
			Vector3 vn = -Vector3.Cross(vr, vu); // normal vector of screen (Right cross Up, - for left handedness)

			float d = -Vector3.Dot(va, vn);         // distance from eye to screen 
			float l = Vector3.Dot(vr, va) * n / d; // distance to left screen edge
			float r = Vector3.Dot(vr, vb) * n / d; // distance to right screen edge
			float b = Vector3.Dot(vu, va) * n / d; // distance to bottom screen edge
			float t = Vector3.Dot(vu, vc) * n / d; // distance to top screen edge

			Matrix4x4 p = new Matrix4x4(); // projection matrix 
			p.m00 = 2.0f * n / (r - l);
			p.m01 = 0;
			p.m02 = (r + l) / (r - l);
			p.m03 = 0;

			p.m10 = 0;
			p.m11 = 2 * n / (t - b);
			p.m12 = (t + b) / (t - b);
			p.m13 = 0;

			p.m20 = 0;
			p.m21 = 0;
			p.m22 = (f + n) / (n - f);
			p.m23 = 2 * f * n / (n - f);

			p.m30 = 0;
			p.m31 = 0;
			p.m32 = -1;
			p.m33 = 0;

			Matrix4x4 rm = new Matrix4x4(); // rotation matrix;
			rm.m00 = vr.x;
			rm.m01 = vr.y;
			rm.m02 = vr.z;
			rm.m03 = 0;

			rm.m10 = vu.x;
			rm.m11 = vu.y;
			rm.m12 = vu.z;
			rm.m13 = 0;

			rm.m20 = vn.x;
			rm.m21 = vn.y;
			rm.m22 = vn.z;
			rm.m23 = 0;

			rm.m30 = 0;
			rm.m31 = 0;
			rm.m32 = 0;
			rm.m33 = 1;

			Matrix4x4 tm = new Matrix4x4(); // translation matrix;
			tm.m00 = 1;
			tm.m01 = 0;
			tm.m02 = 0;
			tm.m03 = -pe.x;

			tm.m10 = 0;
			tm.m11 = 1;
			tm.m12 = 0;
			tm.m13 = -pe.y;

			tm.m20 = 0;
			tm.m21 = 0;
			tm.m22 = 1;
			tm.m23 = -pe.z;

			tm.m30 = 0;
			tm.m31 = 0;
			tm.m32 = 0;
			tm.m33 = 1;

			// set matrices
			camera.projectionMatrix = p;
			camera.worldToCameraMatrix = rm * tm;

			// The original paper puts everything into the projection matrix 
			// (i.e. sets it to p * rm * tm and the other matrix to the identity), 
			// but this doesn't appear to work with Unity's shadow maps.

			bool estimateViewFrustum = true;
			if (estimateViewFrustum)
			{
				// rotate camera to screen for culling to work
				Quaternion q = Quaternion.LookRotation((0.5f * (pb + pc) - pe), vu);
				// look at center of screen
				camera.transform.rotation = q;

				// set fieldOfView to a conservative estimate to make frustum tall enough
				if (camera.aspect >= 1.0)
				{
					camera.fieldOfView = Mathf.Rad2Deg *
						Mathf.Atan(((pb - pa).magnitude + (pc - pa).magnitude) / va.magnitude);
				}
				else
				{
					// take the camera aspect into account to  make the frustum wide enough 
					camera.fieldOfView =
						Mathf.Rad2Deg / camera.aspect *
						Mathf.Atan(((pb - pa).magnitude + (pc - pa).magnitude) / va.magnitude);
				}
			}
		}

		private new Camera camera;
	}
}