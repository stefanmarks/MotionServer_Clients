#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using System.Collections.Generic;
using SentienceLab.Data;

namespace SentienceLab.OSC
{
	[AddComponentMenu("OSC/List Parameter")]
	[RequireComponent(typeof(Parameter_List))]
	public class OSC_ParameterVariable_List : MonoBehaviour, IOSCVariableContainer
	{
		[Tooltip("If not empty, use this name for the index OSC variable (default: ParameterName)")]
		public string IndexNameOverride = "";

		[Tooltip("If not empty, use this name for the index delta OSC variable (default: ParameterName_delta")]
		public string DeltaNameOverride = "";

		[Tooltip("If not empty, use this name for the label OSC variable (default: ParameterName_label")]
		public string LabelNameOverride = "";


		public void Start()
		{
			m_parameter = GetComponent<Parameter_List>();
			m_parameter.OnValueChanged += delegate { OnValueChanged(); };

			if (IndexNameOverride == "") { IndexNameOverride = m_parameter.Name; }
			if (DeltaNameOverride == "") { DeltaNameOverride = IndexNameOverride + "_delta"; }
			if (LabelNameOverride == "") { LabelNameOverride = IndexNameOverride + "_label"; }

			m_indexVariable = new OSC_IntVariable(IndexNameOverride, 0, m_parameter.Count - 1);
			m_indexVariable.OnDataReceived += OnReceivedOSC_Data_Index;

			m_deltaVariable = new OSC_IntVariable(DeltaNameOverride, -1, 1);
			m_deltaVariable.OnDataReceived += OnReceivedOSC_Data_Delta;

			m_labelVariable = new OSC_StringVariable(LabelNameOverride);

			m_updating = false;
		}


		protected void OnReceivedOSC_Data_Index(OSC_Variable var)
		{
			if (!m_updating)
			{
				// prevent recursion
				m_updating = true;

				// select index directly
				m_parameter.SelectedItemIndex = (int)m_indexVariable.Value;
				// update label
				m_labelVariable.Value = m_parameter.SelectedItem.text;
				m_labelVariable.SendUpdate();

				m_updating = false;
			}
		}


		protected void OnReceivedOSC_Data_Delta(OSC_Variable var)
		{
			if (!m_updating)
			{
				// prevent recursion
				m_updating = true;

				// apply delta to index
				m_parameter.SelectedItemIndex += m_deltaVariable.Value;
				// update label
				m_indexVariable.Value = m_parameter.SelectedItemIndex;
				m_labelVariable.Value = m_parameter.SelectedItem.text;
				m_indexVariable.SendUpdate();
				m_labelVariable.SendUpdate();

				m_updating = false;
			}
		}


		protected void OnValueChanged()
		{
			if (!m_updating)
			{
				// prevent recursion
				m_updating = true;

				// apply index and label to OSC variables
				m_indexVariable.Value = m_parameter.SelectedItemIndex;
				m_labelVariable.Value = m_parameter.SelectedItem.text;
				m_indexVariable.SendUpdate();
				m_labelVariable.SendUpdate();

				m_updating = false;
			}
		}


		protected void SetParameterValue(long _value)
		{
			m_parameter.SelectedItemIndex = (int) _value;
		}


		public List<OSC_Variable> GetOSC_Variables()
		{
			return new List<OSC_Variable>(new OSC_Variable[] { m_indexVariable, m_deltaVariable, m_labelVariable });
		}


		private Parameter_List     m_parameter;
		private OSC_IntVariable    m_indexVariable, m_deltaVariable;
		private OSC_StringVariable m_labelVariable;
		private bool               m_updating;
	}
}