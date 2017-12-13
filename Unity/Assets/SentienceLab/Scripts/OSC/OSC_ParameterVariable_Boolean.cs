#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using System.Collections.Generic;
using SentienceLab.Data;

namespace SentienceLab.OSC
{
	[AddComponentMenu("OSC/Boolean Parameter")]
	[RequireComponent(typeof(Parameter_Boolean))]
	public class OSC_ParameterVariable_Boolean : MonoBehaviour, IOSCVariableContainer
	{
		[Tooltip("If not empty, use this name for the OSC variable")]
		public string NameOverride = "";


		public void Start()
		{
			m_parameter = GetComponent<Parameter_Boolean>();
			m_parameter.OnValueChanged += delegate { OnValueChanged(); };

			if (NameOverride == "") { NameOverride = m_parameter.Name; }

			m_variable = new OSC_BoolVariable((NameOverride == "") ? m_parameter.Name : NameOverride);
			m_variable.Value = m_parameter.Value;

			m_variable.OnDataReceived += OnReceivedOSC_Data;

			m_updating = false;
		}


		protected void OnReceivedOSC_Data(OSC_Variable var)
		{
			if (!m_updating)
			{
				m_updating = true;
				m_parameter.Value = m_variable.Value;
				m_updating = false;
			}
		}


		protected void OnValueChanged()
		{
			if (!m_updating)
			{
				m_updating = true;
				m_variable.Value = m_parameter.Value;
				m_variable.SendUpdate();
				m_updating = false;
			}
		}


		public List<OSC_Variable> GetOSC_Variables()
		{
			return new List<OSC_Variable>(new OSC_Variable[] { m_variable });
		}


		private Parameter_Boolean m_parameter;
		private OSC_BoolVariable  m_variable;
		private bool              m_updating;
	}
}