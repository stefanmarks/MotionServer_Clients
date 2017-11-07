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

			m_slider.onValueChanged.AddListener(delegate { SliderValueChanged(); });

			if (Parameter == null)
			{
				// parameter not defined > is it a component?
				Parameter = GetComponent<Parameter_Double>();
			}
			if (Parameter != null)
			{
				Parameter.OnValueChanged += ValueChanged;
				Parameter.OnLimitChanged += ValueChanged;
				m_slider.value = (float)Warp(Parameter.MapTo01(Parameter.Value));
			}
			else
			{
				Debug.LogWarning("Parameter not defined");
			}

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
			if (!m_updating && (Parameter != null))
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
				m_slider.value = (float)Warp(Parameter.MapTo01(Parameter.Value));
				m_updating = false;
			}
		}


		private bool   m_updating;
		private Slider m_slider;
	}
}