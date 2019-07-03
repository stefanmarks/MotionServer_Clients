#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.InputSystem;


namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Controller/Input System/Switch")]

	public class ParameterController_InputSystem_Switch : MonoBehaviour
	{
		[Tooltip("The parameter to control with the input (default: the first component in this game object)")]
		[TypeConstraint(typeof(IParameterAsBoolean))]
		public ParameterBase Parameter;

		[Tooltip("Name of the input that controls this parameter")]
		public InputActionReference Action;

		public enum eMode
		{
			Momentary,
			Momentary_Inverted,
			Toggle
		}

		[Tooltip("Mode of the switch")]
		public eMode Mode;


		public void Start()
		{
			if (Parameter == null)
			{
				Parameter = GetComponent<ParameterBase>();
			}
			if (Parameter != null)
			{
				m_boolean = (IParameterAsBoolean)Parameter;
				if (m_boolean == null)
				{
					Debug.LogWarningFormat("Parameter '{0}' does not provide IParameterAsBoolean interface", Parameter.Name);
					this.enabled = false;
				}
			}
			else
			{
				Debug.LogWarning("Parameter not defined");
				this.enabled = false;
			}

			if (this.enabled)
			{
				if (Action != null)
				{
					Action.action.performed += OnActionPerformed;
					Action.action.canceled  += OnActionCanceled;
					Action.action.Enable();
				}
				else
				{
					Debug.LogWarningFormat("Action not defined for parameter '{0}'", Parameter.Name);
					this.enabled = false;
				}
			}
		}


		private void OnActionPerformed(InputAction.CallbackContext _ctx)
		{
			switch (Mode)
			{
				case eMode.Toggle:             m_boolean.SetBooleanValue(!m_boolean.GetBooleanValue()); break;
				case eMode.Momentary:          m_boolean.SetBooleanValue(true); break;
				case eMode.Momentary_Inverted: m_boolean.SetBooleanValue(false); break;
			}
		}


		private void OnActionCanceled(InputAction.CallbackContext _ctx)
		{
			switch (Mode)
			{
				case eMode.Toggle:             break;
				case eMode.Momentary:          m_boolean.SetBooleanValue(false); break;
				case eMode.Momentary_Inverted: m_boolean.SetBooleanValue(true); break;
			}
		}


		private void OnDestroy()
		{
			if (Action != null)
			{
				Action.action.performed -= OnActionPerformed;
				Action.action.canceled -= OnActionCanceled;
			}
		}


		private IParameterAsBoolean m_boolean;
	}
}