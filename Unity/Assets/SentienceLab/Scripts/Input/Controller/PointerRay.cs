#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using SentienceLab.Data;
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

		[Tooltip("(Optional) Parameter that activates the ray")]
		public Parameter_Boolean activationParameter = null;

		[Tooltip("(Optional) Parameter for relative ray direction")]
		public Parameter_Vector3 rayDirection = null;


		/// <summary>
		/// Interface to implement for objects that need to react to the pointer ray entering/exiting their colliders.
		/// </summary>
		///
		public interface IPointerRayTarget
		{
			void OnPointerEnter(PointerRay _ray);
			void OnPointerExit(PointerRay _ray);
		}


		void Start()
		{
			line = GetComponent<LineRenderer>();
			line.positionCount = 2;
			line.useWorldSpace = true;
			overrideTarget = false;
			activeTarget = null;

			if (activationAction.Trim().Length > 0)
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
			if (activationParameter != null)
			{
				rayEnabled = activationParameter.Value;
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

			if (rayEnabled)
			{
				bool hit = false;
				// construct ray
				Vector3 forward = (rayDirection == null) ? Vector3.forward : rayDirection.Value;
				forward = transform.TransformDirection(forward); // relative forward to "world forward"
				Ray ray = new Ray(transform.position, forward);
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

					// are there collides to check for inside-collisions?
					if (checkInsideColliders.Length > 0)
					{
						// checking inside collides: reverse ray
						Ray reverse = new Ray(ray.origin + ray.direction * rayRange, -ray.direction);
						float minDistance = hit ? rayTarget.distance : rayRange;
						foreach (Collider c in checkInsideColliders)
						{
							RaycastHit hitReverse;
							if (c.Raycast(reverse, out hitReverse, rayRange))
							{
								if (rayRange - hitReverse.distance < minDistance)
								{
									rayTarget = hitReverse;
									minDistance = rayRange - hitReverse.distance;
									hit = true;
								}
							}
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
			
			HandleEvents();
		}


		/// <summary>
		/// Handles events like OnEnter/OnExit if the object that the ray points at
		/// has implemented the IPointerRayTarget interface
		/// </summary>
		/// 
		private void HandleEvents()
		{
			IPointerRayTarget currentTarget = null;
			if (rayTarget.distance > 0 && (rayTarget.transform != null))
			{
				currentTarget = rayTarget.collider.gameObject.GetComponent<IPointerRayTarget>();
			}
			if (currentTarget != activeTarget)
			{
				if (activeTarget != null) activeTarget.OnPointerExit(this);
				activeTarget = currentTarget;
				if (activeTarget != null) activeTarget.OnPointerEnter(this);
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
		/// Checks whether the ray is enabled or not.
		/// </summary>
		/// <returns><c>true</c> when the ray is enabled</returns>
		/// 
		public bool IsEnabled()
		{
			return rayEnabled;
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


		private LineRenderer      line;
		private RaycastHit        rayTarget;
		private bool              overrideTarget;
		private Vector3           overridePoint;
		private InputHandler      handlerActivate;
		private IPointerRayTarget activeTarget;
	}
}
