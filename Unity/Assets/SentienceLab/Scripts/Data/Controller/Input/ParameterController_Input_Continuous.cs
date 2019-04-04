#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using SentienceLab.Input;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Controller/Input/Continuous")]

	public class ParameterController_Input_Continuous : MonoBehaviour
	{
		[Tooltip("The parameter to control with the input (default: the first component in this game object)")]
		[TypeConstraint(typeof(IParameterModify))]
		public ParameterBase Parameter;

		[Tooltip("The index of the value to change (e.g., 0: min, 1: max. Default: 0)")]
		public int ValueIndex = 0;

		[Tooltip("Name of the input that controls this parameter")]
		public string InputName;

		[Tooltip("Factor to change the parameter by per second")]
		public float Multiplier = 1.0f;


		public void Start()
		{
			if (Parameter == null)
			{
				// parameter not defined > is it a component?
				Parameter = GetComponent<ParameterBase>();
			}
			if (Parameter != null)
			{
				m_modify = (IParameterModify)Parameter;
				if (m_modify == null)
				{
					Debug.LogWarning("Parameter can't be modified");
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
			if ((m_modify != null) && (m_handler != null))
			{
				if (m_handler.GetValue() != 0)
				{
					m_modify.ChangeValue(m_handler.GetValue() * Multiplier * Time.deltaTime, ValueIndex);
				}
			}
		}

		private IParameterModify m_modify;
		private InputHandler     m_handler;
	}
}