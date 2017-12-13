#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace SentienceLab.OSC
{
	[AddComponentMenu("OSC/Slider Variable")]
	[RequireComponent(typeof(Slider))]
	public class OSC_SliderVariable : MonoBehaviour, IOSCVariableContainer
	{
		public string variableName = "/slider1";


		public void Start()
		{
			m_slider = GetComponent<Slider>();
			m_slider.onValueChanged.AddListener(delegate { OnSliderChanged(); });

			m_variable       = new OSC_FloatVariable();
			m_variable.Name  = variableName;
			m_variable.Min   = m_slider.minValue;
			m_variable.Max   = m_slider.maxValue;
			m_variable.Value = m_slider.value;

			m_variable.OnDataReceived += OnReceivedOSC_Data;

			m_updating = false;
		}


		protected void OnReceivedOSC_Data(OSC_Variable var)
		{
			if (!m_updating)
			{
				m_updating = true;
				m_slider.value = m_variable.Value;
				m_updating = false;
			}
		}


		protected void OnSliderChanged()
		{
			if (!m_updating)
			{
				m_updating = true;
				m_variable.Value = m_slider.value;
				m_variable.SendUpdate();
				m_updating = false;
			}
		}


		public List<OSC_Variable> GetOSC_Variables()
		{
			return new List<OSC_Variable>(new OSC_Variable[] { m_variable });
		}


		private Slider            m_slider;
		private OSC_FloatVariable m_variable;
		private bool              m_updating;
	}
}