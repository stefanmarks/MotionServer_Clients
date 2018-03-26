#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using SentienceLab.Input;
using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Component for moving a physical object by clicking and moving it.
	/// When clicked, the script will try to maintain the relative position of the rigid body using forces applied to its centre.
	/// If a sound component is attached, this will be played in loop mode.
	/// </summary>
	///
	public class PhysicsManipulator : MonoBehaviour
	{
		[Tooltip("Name of the action to control the physics manipulator")]
		public string actionName  = "trigger";

		[Tooltip("Force per mass unit of the object to apply when moving")]
		public float maxForce = 100;

		[Tooltip("Damping factor (0...1)")]
		[Range(0.0f, 1.0f)]
		public float damping = 0.1f;


		void Start()
		{
			pointerRay = GetComponentInChildren<PointerRay>();
			sound      = GetComponent<AudioSource>();
			actionFire = InputHandler.Find(actionName);
		}


		void Update()
		{
			if (actionFire.IsActivated())
			{
				// trigger pulled: is there any rigid body where the ray points at?
				RaycastHit target;
				if (pointerRay != null)
				{
					// PointerRay used? result is ready
					target = pointerRay.GetRayTarget();
				}
				else
				{
					// no PointerRay > do a quick and simple raycast
					Ray tempRay = new Ray(transform.position, transform.forward);
					Physics.Raycast(tempRay, out target);
				}

				// any rigidbody attached?
				Transform t = target.transform;
				Rigidbody r = (t != null) ? t.GetComponentInParent<Rigidbody>() : null;
				if (r != null)
				{
					// Yes: remember rigid body and its relative position.
					// This relative position is what the script will try to maintain while moving the object
					activeBody = r;
					RigidbodyConstraints c = r.constraints;
					if (c == RigidbodyConstraints.None)
					{
						// body can move freely - apply forces at centre
						relBodyPoint         = Vector3.zero;
						relTargetPoint       = transform.InverseTransformPoint(activeBody.transform.position);
						//relTargetOrientation = Quaternion.Inverse(transform.rotation) * activeBody.transform.rotation;
					}
					else
					{
						// body is restrained - apply forces on contact point
						relBodyPoint         = activeBody.transform.InverseTransformPoint(target.point);
						relTargetPoint       = transform.InverseTransformPoint(target.point);
						//relTargetOrientation = Quaternion.Inverse(transform.rotation) * activeBody.transform.rotation;
					}
					// make target object weightless
					previousGravityFlag = r.useGravity;
					r.useGravity = false;
				}

				if (sound != null)
				{
					sound.Play();
					sound.loop = true;
				}
			}

			if (actionFire.IsDeactivated())
			{
				// fire button released
				if (activeBody != null)
				{
					// trigger released holding a rigid body: turn gravity back on and cease control
					activeBody.useGravity = previousGravityFlag;
					activeBody            = null;
				}
				if (sound != null)
				{
					sound.Stop();
				}
			}

			// moving a rigid body: apply the right force to get that body to the new target position
			if (activeBody != null)
			{
				// Don't use: activeBody.MovePosition(this.transform.TransformPoint(activeBodyPosition));
				Vector3 targetPos = transform.TransformPoint(relTargetPoint);          // target point in world coordinates
				Vector3 bodyPos   = activeBody.transform.TransformPoint(relBodyPoint); // body point in world coordinates
				Vector3 force     = targetPos - bodyPos;                 // how to get to target position
				force -= activeBody.GetPointVelocity(bodyPos) * damping; // apply damping to avoid resonance
				force *= maxForce * activeBody.mass;                     // scale force by mass to treat every object equally
				activeBody.AddForceAtPosition(force, bodyPos);

				//Vector3 axis;
				//float   angle;
				//(transform.rotation * relTargetOrientation).ToAngleAxis(out angle, out axis);
				//activeBody.AddRelativeTorque(axis * angle);
			}
		}


		private PointerRay   pointerRay;
		private AudioSource  sound;
		private InputHandler actionFire;
		private Rigidbody    activeBody;
		private bool         previousGravityFlag;
		private Vector3      relTargetPoint, relBodyPoint;
		//private Quaternion   relTargetOrientation;
	}
}
