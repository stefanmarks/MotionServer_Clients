#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SentienceLab.Data
{
	[RequireComponent(typeof(Dropdown))]
	[AddComponentMenu("Parameter/Controller/UI/Dropdown")]

	public class ParameterController_UI_Dropdown : MonoBehaviour
	{
		public Parameter_List Parameter;


		public void Start()
		{
			m_dropdown = GetComponent<Dropdown>();
			m_dropdown.onValueChanged.AddListener(delegate { DropdownValueChanged(); });

			if (Parameter == null)
			{
				// parameter not defined > is it a component?
				Parameter = GetComponent<Parameter_List>();
			}
			if (Parameter != null)
			{
				Parameter.OnValueChanged += ValueChanged;
				Parameter.OnListChanged  += delegate { m_updateDropdown = true; } ;
				m_dropdown.value = Parameter.SelectedItemIndex;
				m_updateDropdown = true; // force update
			}
			else
			{
				Debug.LogWarning("Parameter not defined");
				this.enabled = false;
			}

			m_updating = false;
		}


		public void Update()
		{
			if (m_updateDropdown)
			{
				UpdateDropdownList();
				m_updateDropdown = false;
			}
		}


		private void UpdateDropdownList()
		{
			List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
			for (int idx = 0; idx < Parameter.Count; idx++)
			{
				Parameter_List.SItem item = Parameter.GetItem(idx);
				Dropdown.OptionData option = new Dropdown.OptionData(item.text, item.image);
				options.Add(option);
			}
			m_dropdown.options = options;
		}


		private void DropdownValueChanged()
		{
			if (!m_updating && (Parameter != null))
			{
				m_updating = true;
				// transfer dropdown value into variable
				Parameter.SelectedItemIndex = m_dropdown.value;
				m_updating = false;
			}
		}


		private void ValueChanged(ParameterBase _parameter)
		{
			if (!m_updating)
			{
				m_updating = true;
				m_dropdown.value = Parameter.SelectedItemIndex;
				m_updating = false;
			}
		}


		private bool     m_updateDropdown;
		private bool     m_updating;
		private Dropdown m_dropdown;
	}
}