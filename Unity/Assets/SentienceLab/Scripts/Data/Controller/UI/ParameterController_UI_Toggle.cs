#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.UI;

namespace SentienceLab.Data
{
	[RequireComponent(typeof(Toggle))]
	[AddComponentMenu("Parameter/Controller/UI/Toggle")]

	public class ParameterController_UI_Toggle : MonoBehaviour
	{
		public Parameter_Boolean Parameter;


		public void Start()
		{
			m_toggle = GetComponent<Toggle>();

			m_toggle.onValueChanged.AddListener(delegate { ToggleValueChanged(); });

			if (Parameter == null)
			{
				// parameter not defined > is it a component?
				Parameter = GetComponent<Parameter_Boolean>();
			}
			if (Parameter != null)
			{
				Parameter.OnValueChanged += ValueChanged;
				m_toggle.isOn = Parameter.Value;
			}
			else
			{
				Debug.LogWarning("Parameter not defined");
			}

			m_updating = false;
		}


		private void ToggleValueChanged()
		{
			if (!m_updating && (Parameter != null))
			{
				m_updating = true;
				// transfer toggle value into variable
				Parameter.Value = m_toggle.isOn;
				m_updating = false;
			}
		}


		private void ValueChanged(ParameterBase _parameter)
		{
			if (!m_updating)
			{
				m_updating = true;
				m_toggle.isOn = Parameter.Value;
				m_updating = false;
			}
		}


		private bool   m_updating;
		private Toggle m_toggle;
	}
}