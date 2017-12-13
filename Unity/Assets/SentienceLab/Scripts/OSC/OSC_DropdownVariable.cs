#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace SentienceLab.OSC
{
	[AddComponentMenu("OSC/Dropdown Variable")]
	[RequireComponent(typeof(Dropdown))]
	public class OSC_DropdownVariable : MonoBehaviour, IOSCVariableContainer
	{
		public string indexVariableName = "/dropdown1";
		public string labelVariableName = "/dropdownName1";


		public void Start()
		{
			m_dropdown = GetComponent<Dropdown>();
			m_dropdown.onValueChanged.AddListener(OnValueChanged);

			m_indexVar = new OSC_FloatVariable(indexVariableName, -0.5f, m_dropdown.options.Count);
			m_indexVar.OnDataReceived += OnReceivedOSC_Data;

			m_labelVar = new OSC_StringVariable(labelVariableName);

			m_updating = false;
		}


		protected void OnReceivedOSC_Data(OSC_Variable var)
		{
			if (!m_updating)
			{
				m_updating = true;
				if (m_indexVar.Value < 0.1f)
				{
					// value < 0: select previous item
					m_dropdown.value--;
				}
				else if ((m_indexVar.Value > 0.1f) && (m_indexVar.Value < 0.9f))
				{
					// value >0 < 1: select next item
					m_dropdown.value++;
				}
				else if ( m_indexVar.Value >= 0)
				{
					// value > 0: select item directly
					m_dropdown.value = (int)m_indexVar.Value;
				}

				m_labelVar.Value = m_dropdown.options[m_dropdown.value].text;
				m_labelVar.SendUpdate();

				m_updating = false;
			}
		}


		protected void OnValueChanged(int value)
		{
			if (!m_updating)
			{
				m_updating = true;
				m_indexVar.Value = m_dropdown.value;
				m_indexVar.SendUpdate();
				m_labelVar.Value = m_dropdown.options[m_dropdown.value].text;
				m_labelVar.SendUpdate();
				m_updating = false;
			}
		}


		public List<OSC_Variable> GetOSC_Variables()
		{
			return new List<OSC_Variable>(new OSC_Variable[] { m_indexVar, m_labelVar });
		}


		private Dropdown           m_dropdown;
		private OSC_FloatVariable  m_indexVar;
		private OSC_StringVariable m_labelVar;
		private bool               m_updating;
	}
}