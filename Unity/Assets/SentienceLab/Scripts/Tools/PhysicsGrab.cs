#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using SentienceLab.Input;

namespace SentienceLab.Tools
{
	[AddComponentMenu("Physics/Controller Grab")]
	[RequireComponent(typeof(Collider))]
	public class PhysicsGrab : MonoBehaviour
	{
		[Tooltip("Name of the input that starts the grab action")]
		public string InputName;

		[Tooltip("Force to apply")]
		public float Force = 10;

		[Tooltip("Tag of elements that can be grabbed")]
		public string CanGrabTag = "grab";

		[Tooltip("Default rigidbody that can be grabbed without having a collider (e.g., the only main object in the scene)")]
		public Rigidbody DefaultRigidBody = null;


		public void Start()
		{
			m_handlerActive = InputHandler.Find(InputName);
			if (m_handlerActive == null)
			{
				Debug.LogWarning("Could not find input handler for '" + InputName + "'");
				this.enabled = false;
			}
			m_candidate = DefaultRigidBody;
		}


		public void Update()
		{
			if (m_handlerActive.IsActivated())
			{
				m_activeBody = m_candidate;
				if (m_activeBody != null)
				{
					m_localGrabPoint = m_activeBody.transform.InverseTransformPoint(this.transform.position);
				}
			}
			else if (m_handlerActive.IsDeactivated())
			{
				m_activeBody = null;
			}
		}


		public void FixedUpdate()
		{
			if (m_handlerActive.IsActive() && (m_activeBody != null))
			{
				// calculate necessary velocity to maintain grab
				Vector3 newPos    = this.transform.position;
				Vector3 grabPoint = m_activeBody.transform.TransformPoint(m_localGrabPoint);
				Vector3 deltaPos  = newPos - grabPoint;
				
				// compensate by body movement
				Vector3 velocity = m_activeBody.GetPointVelocity(grabPoint);
				deltaPos -= 0.5f * velocity;
				if (deltaPos.magnitude > 1) deltaPos.Normalize();
				Vector3 force = deltaPos * Force;
				m_activeBody.AddForceAtPosition(force, this.transform.position, ForceMode.Force);
			}
		}


		public void OnTriggerEnter(Collider other)
		{
			if (other.gameObject.tag.Equals(CanGrabTag))
			{
				m_candidate = other.GetComponentInParent<Rigidbody>();
			}
		}


		public void OnTriggerExit(Collider other)
		{
			m_candidate = DefaultRigidBody;
		}


		private InputHandler m_handlerActive;
		private Vector3      m_localGrabPoint;
		private Rigidbody    m_candidate, m_activeBody;
	}
}