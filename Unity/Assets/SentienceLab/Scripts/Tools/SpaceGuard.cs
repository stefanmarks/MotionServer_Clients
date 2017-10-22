#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using SentienceLab.MoCap;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace SentienceLab
{
	/// <summary>
	/// Script for fading in geometry that signals the limits of the tracking volume.
	/// </summary>
	/// 
	[AddComponentMenu("VR/Space Guard")]
	public class SpaceGuard : MonoBehaviour
	{
		[Tooltip("Automatically include the main camera in the list of guarded objects.")]
		public bool includeMainCamera = true;

		[Tooltip("Automatically include all motion captured objects.")]
		public bool includeMoCapObjects = true;

		[Tooltip("Additional objects that need to be guarded to stay within the walls.")]
		public List<Transform> guardedObjects = new List<Transform>();

		[Tooltip("Material to use for rendering the walls.")]
		public Material wallMaterial;

		[Tooltip("The height of the walls.")]
		public float wallHeight = 2;

		[Tooltip("The minimum distance that the falls should start to fade in.")]
		public float fadeInDistance = 0.5f;

		[Tooltip("The name of the material parameter to modify based on the distance.")]
		public string colourParameterName = "_TintColor";

		[Tooltip("Colour gradient for fading in the walls")]
		public Gradient colourGradient;

		[Tooltip("Material for floor indicator (null for no floor)")]
		public Material floorMaterial = null;

		[Tooltip("Floor offset along Y axis")]
		public float floorOffsetY = 0.01f;


		/// <summary>
		/// Called at start of the script.
		/// Gathers all children that have renderers as walls.
		/// </summary>
		/// 
		void Start()
		{
			// is the camera already in the list?
			if (includeMainCamera && !guardedObjects.Contains(Camera.main.transform))
			{
				guardedObjects.Add(Camera.main.transform);
			}

			// automatically add all MoCap objects
			if (includeMoCapObjects)
			{
				MoCapObject[] objects = FindObjectsOfType<MoCapObject>();
				foreach (MoCapObject o in objects)
				{
					guardedObjects.Add(o.transform);
				}
			}

			// create walls
			walls = new List<MeshRenderer>();
			CreateSpaceGuardWalls();
		}


		private void CreateSpaceGuardWalls()
		{
			switch (ConfigurationManager.GetConfiguration())
			{
				case ConfigurationManager.Configuration.HTC_Vive:
					CVRChaperone chaperone = OpenVR.Chaperone;
					HmdQuad_t area = new HmdQuad_t();
					chaperone.GetPlayAreaRect(ref area);
					CreateWall(area.vCorners0.v0, area.vCorners0.v2, area.vCorners1.v0, area.vCorners1.v2, "Wall1");
					CreateWall(area.vCorners1.v0, area.vCorners1.v2, area.vCorners2.v0, area.vCorners2.v2, "Wall2");
					CreateWall(area.vCorners2.v0, area.vCorners2.v2, area.vCorners3.v0, area.vCorners3.v2, "Wall3");
					CreateWall(area.vCorners3.v0, area.vCorners3.v2, area.vCorners0.v0, area.vCorners0.v2, "Wall4");
					CreateFloor(area.vCorners0.v0, area.vCorners0.v2, area.vCorners2.v0, area.vCorners2.v2);
					chaperone.ForceBoundsVisible(false);
					break;

				case ConfigurationManager.Configuration.MoCapRoom:
					CreateWall(-2, -2, 2, -2, "Front");
					CreateWall(2, -2, 2, 2, "Right");
					CreateWall(2, 2, -2, 2, "Back");
					CreateWall(-2, 2, -2, -2, "Left");
					CreateFloor(-2, -2, 2, 2);
					break;

				default:
					break;
			}
		}


		private void CreateWall(float x1, float z1, float x2, float z2, string name)
		{
			// Debug.Log(x1 + "/" + z1 + " > " + x2 + " / " + z2);
			GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			Destroy(quad.GetComponent<Collider>());  // no need for physics
			Destroy(quad.GetComponent<Rigidbody>());

			Vector3 corner1 = new Vector3(x1, 0, z1);
			Vector3 corner2 = new Vector3(x2, 0, z2);
			Vector3 wall = corner2 - corner1;
			Vector3 centre = 0.5f * (corner2 + corner1) + new Vector3(0, wallHeight / 2, 0);
			quad.name = name;
			quad.transform.parent = this.transform;
			quad.transform.localPosition = centre;
			quad.transform.localScale = new Vector3(wall.magnitude, wallHeight, 1);
			quad.transform.localRotation = Quaternion.AngleAxis(Mathf.Rad2Deg * Mathf.Atan2(centre.x, centre.z), Vector3.up);

			MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
			renderer.material = wallMaterial;
			Vector2 scale = renderer.material.GetTextureScale("_MainTex");
			scale.Scale(new Vector2(Mathf.Max(1, Mathf.Round(wall.magnitude)), wallHeight));
			renderer.material.SetTextureScale("_MainTex", scale);

			walls.Add(renderer);
		}


		private void CreateFloor(float x1, float z1, float x2, float z2)
		{
			if (floorMaterial == null) return;

			// Debug.Log(x1 + "/" + z1 + " > " + x2 + " / " + z2);
			GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			Destroy(quad.GetComponent<Collider>());  // no need for physics
			Destroy(quad.GetComponent<Rigidbody>());

			Vector3 corner1 = new Vector3(x1, floorOffsetY, z1);
			Vector3 corner2 = new Vector3(x2, floorOffsetY, z2);
			Vector3 floor = corner2 - corner1;
			Vector3 centre = 0.5f * (corner2 + corner1);
			quad.name = "Floor";
			quad.transform.parent = this.transform;
			quad.transform.localPosition = centre;
			quad.transform.localScale = new Vector3(floor.x, floor.z, 1);
			quad.transform.localRotation = Quaternion.AngleAxis(90, Vector3.right);

			MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
			renderer.material = floorMaterial;
		}


		/// <summary>
		/// Called one per frame.
		/// Updates the visibility of the walls based on the distance of the guarded object from them.
		/// </summary>
		/// 
		void Update()
		{
			foreach (MeshRenderer wall in walls)
			{
				// calculate minimum distance of guarded objects against all the walls
				Vector3 pos = wall.transform.position;
				Vector3 normal = wall.transform.forward;
				float dist = float.PositiveInfinity;
				foreach (Transform guardedObject in guardedObjects)
				{
					if (guardedObject.gameObject.activeInHierarchy)
					{
						dist = Mathf.Min(dist, -Vector3.Dot(normal, guardedObject.position - pos));
					}
				}
				Color wallColour = wall.material.GetColor(colourParameterName);

				// can't be further away from the wall than "in" it
				if (dist < 0) { dist = 0; }

				// too far away and wall not visible: don't worry changing the material
				if ((dist > fadeInDistance * 1.1) && (wallColour.a == 0))
					continue;

				// calculate colour to apply to the wall
				float fade = 1 - (dist / fadeInDistance);
				wallColour = colourGradient.Evaluate(fade);
				wall.material.SetColor(colourParameterName, wallColour);
			}
		}

		private List<MeshRenderer> walls;
	}
}
