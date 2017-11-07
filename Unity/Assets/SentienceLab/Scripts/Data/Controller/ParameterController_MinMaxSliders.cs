#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.UI;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Controller/MinMax Sliders")]

	public class ParameterController_MinMaxSliders : MonoBehaviour
	{
		public Parameter_DoubleRange Parameter;
		public Slider minimumSlider;
		public Slider maximumSlider;
		public float  PowerFactor = 1;


		public void Start()
		{
			if (Parameter == null)
			{
				// parameter not defined > is it a component?
				Parameter = GetComponent<Parameter_DoubleRange>();
			}
			if (Parameter != null)
			{
				Parameter.OnValueChanged += ValueChanged;
				Parameter.OnLimitChanged += ValueChanged;
			}
			else
			{
				Debug.LogWarning("Parameter not defined");
			}
			
			maximumSlider.onValueChanged.AddListener(delegate { MaxSliderValueChanged(); });
			minimumSlider.onValueChanged.AddListener(delegate { MinSliderValueChanged(); });

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


		private void MinSliderValueChanged()
		{
			if (!m_updating && (Parameter != null))
			{
				m_updating = true;
				// transfer slider value into variable
				double min = Parameter.MapFrom01(Unwarp(minimumSlider.value));
				Parameter.ValueMin = min;
				// transfer possible corrected max value back into slider
				double max = Parameter.ValueMax;
				maximumSlider.value = (float)Warp(Parameter.MapTo01(max));
				m_updating = false;
			}
		}


		private void MaxSliderValueChanged()
		{
			if (!m_updating && (Parameter != null))
			{
				m_updating = true;
				// transfer slider value into variable
				double max = Parameter.MapFrom01(Unwarp(maximumSlider.value));
				Parameter.ValueMax = max;
				// transfer possible corrected min value back into slider
				double min = Parameter.ValueMin;
				minimumSlider.value = (float)Warp(Parameter.MapTo01(min));
				m_updating = false;
			}
		}


		private void ValueChanged(ParameterBase _parameter)
		{
			if (!m_updating)
			{
				m_updating = true;
				minimumSlider.value = (float)Warp(Parameter.MapTo01(Parameter.ValueMin));
				maximumSlider.value = (float)Warp(Parameter.MapTo01(Parameter.ValueMax));
				m_updating = false;
			}
		}


		private bool m_updating;
	}
}