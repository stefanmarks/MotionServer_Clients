#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.UI;

namespace SentienceLab.Data
{
	[RequireComponent(typeof(Text))]
	[AddComponentMenu("Parameter/Controller/UI/Text")]

	public class ParameterController_UI_Text : MonoBehaviour
	{
		[Tooltip("The parameter to convert into text")]
		public ParameterBase Parameter;

		[Tooltip("Number format string")]
		public string NumberFormat = "{0:0.0}...{1:0.0}"; // see https://msdn.microsoft.com/en-us/library/dwhawy9k(v=vs.110).aspx for formats



		public void Start()
		{
			if (Parameter == null)
			{
				// parameter not defined > is it a component?
				Parameter = GetComponent<ParameterBase>();
			}
			if (Parameter != null)
			{
				Parameter.OnValueChanged += ValueChanged;
			}
			else
			{
				Debug.LogWarning("Parameter not defined");
				this.enabled = false;
			}

			m_textComponent = GetComponent<Text>();
			m_prefix        = m_textComponent.text;
			
		}


		private void ValueChanged(ParameterBase _parameter)
		{
			m_textComponent.text = m_prefix + _parameter.ToFormattedString(NumberFormat);
		}


		private Text   m_textComponent;
		private string m_prefix;
	}
}