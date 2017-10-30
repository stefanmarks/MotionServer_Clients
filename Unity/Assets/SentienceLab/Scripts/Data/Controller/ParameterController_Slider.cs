#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.UI;

namespace SentienceLab.Data
{
	[RequireComponent(typeof(Slider))]
	[AddComponentMenu("Parameter/Controller/Slider")]

	public class ParameterController_Slider : MonoBehaviour
	{
		public Parameter_Double Parameter;

		public float PowerFactor = 1;


		public void Start()
		{
			m_slider = GetComponent<Slider>();
			m_slider.maxValue = 1;
			m_slider.minValue = 0;
			m_slider.value = (float) Warp(Parameter.MapTo01(Parameter.Value));

			Parameter.OnValueChanged += ValueChanged;
			Parameter.OnLimitChanged += ValueChanged;

			m_slider.onValueChanged.AddListener(delegate { SliderValueChanged(); });

			m_updating = false;
		}


		private double Warp(double v)
		{
			return System.Math.Pow(v, PowerFactor);
		}


		private double Unwarp(double v)
		{
			return System.Math.Pow(v, 1 / PowerFactor);
		}


		private void SliderValueChanged()
		{
			if (!m_updating)
			{
				m_updating = true;
				// transfer slider value into variable
				Parameter.Value = Parameter.MapFrom01(Unwarp(m_slider.value));
				m_updating = false;
			}
		}


		private void ValueChanged(ParameterBase _parameter)
		{
			if (!m_updating)
			{
				m_updating = true;
				Parameter_Double parameter = (Parameter_Double)_parameter;
				m_slider.value = (float)Warp(parameter.MapTo01(parameter.Value));
				m_updating = false;
			}
		}


		public void Update()
		{
			// nothing to do here
		}


		private bool   m_updating;
		private Slider m_slider;
	}
}