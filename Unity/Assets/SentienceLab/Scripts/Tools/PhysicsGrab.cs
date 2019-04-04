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
			if (m_handlerActive.IsActivated() && (m_candidate != null))
			{
				m_activeBody = m_candidate;
				if (m_activeBody != null)
				{
					m_startPoint = this.transform.position;
					m_grabPoint  = m_activeBody.transform.InverseTransformPoint(m_startPoint);
				}
			}
			else if (m_handlerActive.IsActive() && (m_activeBody != null))
			{
				Vector3 newPos   = this.transform.position;
				Vector3 deltaPos = newPos - m_activeBody.transform.TransformPoint(m_grabPoint);
				deltaPos -= m_activeBody.velocity * 0.2f; // don't overshoot
				Vector3 vel = deltaPos / Time.deltaTime;
				m_activeBody.AddForceAtPosition(vel, this.transform.position, ForceMode.Acceleration);
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
		private Vector3      m_startPoint, m_grabPoint;
		private Rigidbody    m_candidate, m_activeBody;
		private Rigidbody    m_noColliderCandidate;
	}
}