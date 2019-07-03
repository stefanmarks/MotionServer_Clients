#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using SentienceLab.Input;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Controller/Input/Switch")]

	public class ParameterController_Input_Switch : MonoBehaviour
	{
		[Tooltip("The parameter to control with the input (default: the first component in this game object)")]
		[TypeConstraint(typeof(IParameterAsBoolean))]
		public ParameterBase Parameter;

		[Tooltip("Name of the input that controls this parameter")]
		public string InputName;

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

			m_handler = InputHandler.Find(InputName);
		}


		public void Update()
		{
			if ((m_boolean != null) && (m_handler != null))
			{
				if (m_handler.IsActivated())
				{
					switch (Mode)
					{
						case eMode.Toggle:             m_boolean.SetBooleanValue(!m_boolean.GetBooleanValue()); break;
						case eMode.Momentary:          m_boolean.SetBooleanValue(true); break;
						case eMode.Momentary_Inverted: m_boolean.SetBooleanValue(false); break;
					}
				}
				else if (m_handler.IsDeactivated())
				{
					switch (Mode)
					{
						case eMode.Momentary:          m_boolean.SetBooleanValue(false); break;
						case eMode.Momentary_Inverted: m_boolean.SetBooleanValue(true); break;
					}
				}
			}
		}


		private IParameterAsBoolean m_boolean;
		private InputHandler        m_handler;
	}
}