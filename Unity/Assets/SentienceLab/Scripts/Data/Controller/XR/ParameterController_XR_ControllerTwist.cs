#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using SentienceLab.Input;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Controller/XR/Controller Twist")]

	public class ParameterController_XR_ControllerTwist : MonoBehaviour
	{
		[Tooltip("The parameter to control with the input")]
		public Parameter_Double Parameter;

		[Tooltip("Name of the input that starts the twist")]
		public string InputName;

		[Tooltip("Curve for the change of the parameter in units/s based on the rotation angle")]
		public AnimationCurve Curve = AnimationCurve.Constant(-180, 180, 1);


		public void Start()
		{
			if (Parameter == null)
			{
				// parameter not defined > is it a component?
				Parameter = GetComponent<Parameter_Double>();
			}
			if (Parameter == null)
			{
				Debug.LogWarning("Parameter not defined");
				this.enabled = false;
			}

			m_handlerActive = InputHandler.Find(InputName);
		}


		public void Update()
		{
			if ((Parameter != null) && (m_handlerActive != null))
			{
				if (m_handlerActive.IsActivated())
				{
					m_startRotation = transform.rotation.eulerAngles;
				}
				else if (m_handlerActive.IsActive())
				{
					Vector3 newRot = transform.rotation.eulerAngles;
					// find delta rotation
					float angle = m_startRotation.z - newRot.z; // minus: clockwise = positive number
					// account for 360 degree jump
					while (angle < -180) angle += 360;
					while (angle > +180) angle -= 360;
					// This should only work when controller points horizontally
					// > diminish when |Y| component of forwards vector increases
					angle *= 1 - Mathf.Abs(transform.forward.y);
					// actually change parameter
					Parameter.ChangeValue(Time.deltaTime * Curve.Evaluate(angle), 0);
				}
			}
		}

		private InputHandler m_handlerActive;
		private Vector3      m_startRotation;
	}
}