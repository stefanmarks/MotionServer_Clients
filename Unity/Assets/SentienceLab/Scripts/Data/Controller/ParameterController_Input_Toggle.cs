#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using SentienceLab.Input;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Controller/Input (toggle)")]

	public class ParameterController_Input_Toggle : MonoBehaviour
	{
		[Tooltip("The parameter to control with the input (default: the first component in this game object)")]
		public ParameterBase Parameter;

		[Tooltip("Name of the input that controls this parameter")]
		public string InputName;


		public void Start()
		{
			if (Parameter == null)
			{
				Parameter = GetComponent<ParameterBase>();
			}
			if (Parameter != null)
			{
				m_toggleParameter = (IParameterToggle)Parameter;
				if (m_toggleParameter == null)
				{
					Debug.LogWarning("Parameter cannot be toggled");
				}
			}
			else
			{
				Debug.LogWarning("Parameter not defined");
			}

			m_handler = InputHandler.Find(InputName);
		}


		public void Update()
		{
			if ((m_toggleParameter != null) && (m_handler != null))
			{
				if (m_handler.IsActivated())
				{
					m_toggleParameter.Toggle();
				}
			}
		}


		private IParameterToggle m_toggleParameter;
		private InputHandler     m_handler;
	}
}