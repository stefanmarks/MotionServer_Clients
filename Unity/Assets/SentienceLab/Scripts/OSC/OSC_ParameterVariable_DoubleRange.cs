#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using System.Collections.Generic;
using SentienceLab.Data;

namespace SentienceLab.OSC
{
	[AddComponentMenu("OSC/Double Range Parameter")]
	[RequireComponent(typeof(Parameter_DoubleRange))]
	public class OSC_ParameterVariable_DoubleRange : MonoBehaviour, IOSCVariableContainer
	{
		[Tooltip("If not empty, use this name for the OSC variable")]
		public string NameOverride = "";

		[Tooltip("If checked, the OSC variable will send/receive values between 0 and 1 and scale them to the min/max range of the parameter")]
		public bool Normalise = false;


		public void Start()
		{
			m_parameter = GetComponent<Parameter_DoubleRange>();
			m_parameter.OnValueChanged += delegate { OnValueChanged(); };

			if (NameOverride == "") { NameOverride = m_parameter.Name; }

			m_variableMin = new OSC_FloatVariable(NameOverride + "/min",
				(float) (Normalise ? 0 : m_parameter.LimitMin),
				(float) (Normalise ? 1 : m_parameter.LimitMax));
			m_variableMin.Value = (float) GetParameterValueMin();
			m_variableMin.OnDataReceived += OnReceivedOSC_Data;

			m_variableMax = new OSC_FloatVariable(NameOverride + "/max",
				(float)(Normalise ? 0 : m_parameter.LimitMin),
				(float)(Normalise ? 1 : m_parameter.LimitMax));
			m_variableMax.Value = (float)GetParameterValueMax();
			m_variableMax.OnDataReceived += OnReceivedOSC_Data;

			m_updating = false;
		}


		protected void OnReceivedOSC_Data(OSC_Variable var)
		{
			if (!m_updating)
			{
				m_updating = true;
				SetParameterValueMin(m_variableMin.Value);
				SetParameterValueMax(m_variableMax.Value);
				m_updating = false;
			}
		}


		protected void OnValueChanged()
		{
			if (!m_updating)
			{
				m_updating = true;
				m_variableMin.Value = (float)GetParameterValueMin();
				m_variableMin.SendUpdate();
				m_variableMax.Value = (float)GetParameterValueMax();
				m_variableMax.SendUpdate();
				m_updating = false;
			}
		}


		protected double GetParameterValueMin()
		{
			double value = m_parameter.ValueMin;
			if (Normalise) value = m_parameter.MapTo01(value);
			return value;
		}


		protected void SetParameterValueMin(double _value)
		{
			if (Normalise)
			{
				_value = m_parameter.MapFrom01(_value);
			}
			m_parameter.ValueMin = _value;
		}


		protected double GetParameterValueMax()
		{
			double value = m_parameter.ValueMax;
			if (Normalise) value = m_parameter.MapTo01(value);
			return value;
		}


		protected void SetParameterValueMax(double _value)
		{
			if (Normalise)
			{
				_value = m_parameter.MapFrom01(_value);
			}
			m_parameter.ValueMax = _value;
		}


		public List<OSC_Variable> GetOSC_Variables()
		{
			return new List<OSC_Variable>(new OSC_Variable[] { m_variableMin, m_variableMax });
		}


		private Parameter_DoubleRange  m_parameter;
		private OSC_FloatVariable      m_variableMin, m_variableMax;
		private bool                   m_updating;
	}
}