#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.UI;
using System;

namespace SentienceLab.Data
{
	[RequireComponent(typeof(Text))]
	[AddComponentMenu("Parameter/Controller/Text")]

	public class ParameterController_Text : MonoBehaviour
	{
		[Tooltip("The parameter to convert into text")]
		public ParameterBase Parameter;

		[Tooltip("Number format string")]
		public string numberFormat = "{0:0.0}...{1:0.0}"; // see https://msdn.microsoft.com/en-us/library/dwhawy9k(v=vs.110).aspx for formats



		public void Start()
		{
			m_textComponent = GetComponent<Text>();
			m_prefix = m_textComponent.text;
			Parameter.OnValueChanged += ValueChanged;
		}


		private void ValueChanged(ParameterBase _parameter)
		{
			m_textComponent.text = m_prefix + _parameter.ToFormattedString(numberFormat);
		}


		public void Update()
		{
			// nothing to do here
		}


		private Text   m_textComponent;
		private string m_prefix;
	}
}