#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace SentienceLab.OSC
{
	[AddComponentMenu("OSC/Button Variable")]
	[RequireComponent(typeof(Button))]
	public class OSC_ButtonVariable : MonoBehaviour, IOSCVariableContainer
	{
		public string variableName = "/button1";


		public void Start()
		{
			m_button = GetComponent<Button>();
			m_button.onClick.AddListener(OnButtonClicked);

			m_variable = new OSC_BoolVariable(variableName);
			m_variable.OnDataReceived += OnReceivedOSC_Data;

			m_updating = false;
		}


		protected void OnReceivedOSC_Data(OSC_Variable var)
		{
			if (!m_updating)
			{
				m_updating = true;
				if (m_variable.Value)
				{
					m_button.onClick.Invoke();
				}
				m_updating = false;
			}
		}


		protected void OnButtonClicked()
		{
			if (!m_updating)
			{
				m_updating = true;
				m_variable.Value = true;
				m_variable.SendUpdate();
				m_variable.Value = false;
				m_variable.SendUpdate();
				m_updating = false;
			}
		}


		public List<OSC_Variable> GetOSC_Variables()
		{
			return new List<OSC_Variable>(new OSC_Variable[] { m_variable });
		}


		private Button           m_button;
		private OSC_BoolVariable m_variable;
		private bool             m_updating;
	}
}