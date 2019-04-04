#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using System.Collections.Generic;
using SentienceLab.Data;

namespace SentienceLab.OSC
{
	[AddComponentMenu("OSC/Vector3 Parameter")]
	[RequireComponent(typeof(Parameter_Vector3))]
	public class OSC_ParameterVariable_Vector3 : MonoBehaviour, IOSCVariableContainer
	{
		[Tooltip("If not empty, use this name for the OSC variable")]
		public string NameOverride = "";

		[Tooltip("If checked, the OSC variable will send/receive values between 0 and 1 and scale them to the min/max range of the parameter")]
		public bool Normalise = false;


		public void Start()
		{
			m_parameter = GetComponent<Parameter_Vector3>();
			m_parameter.OnValueChanged += delegate { OnValueChanged(); };

			if (NameOverride == "") { NameOverride = m_parameter.Name; }

			m_variable = new OSC_Vector3Variable(NameOverride);
			m_variable.Value = GetParameterValue();
			m_variable.OnDataReceived += OnReceivedOSC_Data;
			
			m_updating = false;
		}


		protected void OnReceivedOSC_Data(OSC_Variable var)
		{
			if (!m_updating)
			{
				m_updating = true;
				SetParameterValue(m_variable.Value);
				m_updating = false;
			}
		}


		protected void OnValueChanged()
		{
			if (!m_updating)
			{
				m_updating = true;
				m_variable.Value = GetParameterValue();
				m_updating = false;
			}
		}


		protected Vector3 GetParameterValue()
		{
			Vector3 value = m_parameter.Value;
			if (Normalise) value = m_parameter.MapTo01(value);
			return value;
		}


		protected void SetParameterValue(Vector3 _value)
		{
			if (Normalise)
			{
				_value = m_parameter.MapFrom01(_value);
			}
			m_parameter.Value = _value;
		}


		public List<OSC_Variable> GetOSC_Variables()
		{
			return new List<OSC_Variable>(new OSC_Variable[] { m_variable });
		}


		private Parameter_Vector3   m_parameter;
		private OSC_Vector3Variable m_variable;
		private bool                m_updating;
	}
}