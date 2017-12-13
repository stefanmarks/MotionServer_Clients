﻿#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using SentienceLab.Input;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Controller/Input (discrete)")]

	public class ParameterController_Input_Discrete : MonoBehaviour
	{
		[Tooltip("The parameter to control with the input (default: the first component in this game object)")]
		public ParameterBase Parameter;

		[Tooltip("The index of the value to change (e.g., 0: min, 1: max. Default: 0)")]
		public int ValueIndex = 0;

		[Tooltip("Name of the input that increases this parameter")]
		public string InputNameIncrease;

		[Tooltip("Name of the input that decreases this parameter")]
		public string InputNameDecrease;

		[Tooltip("Factor to change the parameter by per step")]
		public int Multiplier = 1;


		public void Start()
		{
			if (Parameter == null)
			{
				// parameter not defined > is it a component?
				Parameter = GetComponent<ParameterBase>();
			}
			if (Parameter != null)
			{
				m_modifyParameter = (IParameterModify)Parameter;
				if (m_modifyParameter == null)
				{
					Debug.LogWarning("Parameter cannot be modified");
				}
			}
			else
			{
				Debug.LogWarning("Parameter not defined");
			}

			m_handlerInc = InputHandler.Find(InputNameIncrease);
			m_handlerDec = InputHandler.Find(InputNameDecrease);
		}


		public void Update()
		{
			if (m_modifyParameter != null)
			{
				if ((m_handlerInc != null) && m_handlerInc.IsActivated())
				{
					m_modifyParameter.ChangeValue(Multiplier, ValueIndex);
				}
				if ((m_handlerDec != null) && m_handlerDec.IsActivated())
				{
					m_modifyParameter.ChangeValue(-Multiplier, ValueIndex);
				}
			}
		}


		private IParameterModify m_modifyParameter;
		private InputHandler     m_handlerInc, m_handlerDec;
	}
}