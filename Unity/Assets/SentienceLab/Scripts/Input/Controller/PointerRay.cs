#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using SentienceLab.Input;
using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Component for controlling a laser-like ray to point at objects in the scene.
	/// This component can be queried as to what it is pointing at.
	/// </summary>
	///

	[RequireComponent(typeof(LineRenderer))]

	public class PointerRay : MonoBehaviour
	{
		[Tooltip("Enable or disable the ray")]
		public bool rayEnabled = true;

		[Tooltip("Maximum range of the ray")]
		public float rayRange = 100.0f;

		[Tooltip("List of tags that the pointer reacts to (e.g., 'floor')")]
		public string[] tagList = { };

		[Tooltip("List of colliders to check for inside-out collisions")]
		public Collider[] checkInsideColliders = { };

		[Tooltip("Object to render at the point where the ray meets another game object (optional)")]
		public Transform activeEndPoint = null;

		[Tooltip("(Optional) Action to activate the ray")]
		public string activationAction = "";


		void Start()
		{
			line = GetComponent<LineRenderer>();
			line.numPositions = 2;
			line.useWorldSpace = true;
			overrideTarget = false;

			if ( activationAction.Trim().Length > 0 )
			{
				handlerActivate = InputHandler.Find(activationAction);
				rayEnabled = false;
			}
		}


		void LateUpdate()
		{
			// assume nothing is hit at first
			rayTarget.distance = 0;

			if (handlerActivate != null)
			{
				rayEnabled = handlerActivate.IsActive();
			}

			// change in enabled flag
			if (line.enabled != rayEnabled)
			{
				line.enabled = rayEnabled;
				if ((activeEndPoint != null) && !rayEnabled)
				{
					activeEndPoint.gameObject.SetActive(false);
				}
			}

			if (!line.enabled) return; // if ray is disabled, bail out right now

			bool hit = false;
			// construct ray
			Ray ray = new Ray(transform.position, transform.forward);
			Vector3 end = ray.origin + ray.direction * rayRange;
			line.SetPosition(0, ray.origin);
			Debug.DrawLine(ray.origin, end, Color.red);

			if (!overrideTarget)
			{
				// do raycast
				hit = Physics.Raycast(ray, out rayTarget, rayRange);

				// test tags
				if (hit && (tagList.Length > 0))
				{
					hit = false;
					foreach (string tag in tagList)
					{
						if (rayTarget.transform.tag.CompareTo(tag) == 0)
						{
							hit = true;
							break;
						}
					}
					if (!hit)
					{
						// tag test negative > reset raycast structure
						Physics.Raycast(ray, out rayTarget, 0);
					}
				}

				Ray reverse = new Ray(ray.origin + ray.direction * rayRange, -ray.direction);
				foreach (Collider c in checkInsideColliders)
				{
					RaycastHit hitReverse;
					if ( c.Raycast(reverse, out hitReverse, rayRange) && (rayRange - hitReverse.distance < (hit ? rayTarget.distance : rayRange)) )
					{
						rayTarget = hitReverse;
						hit = true;
					}
				}
			}
			else
			{
				Physics.Raycast(ray, out rayTarget, 0); // reset structure
				rayTarget.point = overridePoint;        // override point
				hit = true;
			}

			if (hit)
			{
				// hit something > draw ray to there and render end point object
				line.SetPosition(1, rayTarget.point);
				if (activeEndPoint != null)
				{
					activeEndPoint.position = rayTarget.point;
					activeEndPoint.gameObject.SetActive(true);
				}
			}
			else
			{
				// hit nothing > draw ray to end and disable end point object
				line.SetPosition(1, end);
				if (activeEndPoint != null)
				{
					activeEndPoint.gameObject.SetActive(false);
				}
			}
		}


		/// <summary>
		/// Returns the current target of the ray.
		/// </summary>
		/// <returns>the last raycastHit result</returns>
		/// 
		public RaycastHit GetRayTarget()
		{
			return rayTarget;
		}


		/// <summary>
		/// Sets the current target of the ray.
		/// </summary>
		/// 
		public void OverrideRayTarget(Vector3 pos)
		{
			if (pos.Equals(Vector3.zero))
			{
				overrideTarget = false;
			}
			else
			{
				overrideTarget = true;
				overridePoint  = pos;
			}
		}


		private LineRenderer line;
		private RaycastHit   rayTarget;
		private bool         overrideTarget;
		private Vector3      overridePoint;
		private InputHandler handlerActivate;
	}
}
