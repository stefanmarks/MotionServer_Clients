#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Boolean")]
	public class Parameter_Boolean : ParameterBase, IParameterAsBoolean
	{
		/// <summary>
		/// The actual value.
		/// </summary>
		///
		public bool Value
		{
			get { return m_value; }
			set
			{
				m_checkForChange |= (value != m_value);
				m_value = value;
			}
		}


		/// <summary>
		/// Creates a formatted string.
		/// The value is passed to the formatting function as parameters #0.
		/// </summary>
		/// <param name="_formatString">the format string to use</param>
		/// <returns>the formatted string</returns>
		///
		public override string ToFormattedString(string _formatString)
		{
			return string.Format(_formatString, m_value);
		}


		protected override void CheckForChange()
		{
			// this is only called when the boolean has really changed, so no "old value" check necessary
			InvokeOnValueChanged();
		}


		public bool GetBooleanValue()
		{
			return Value;
		}


		public void SetBooleanValue(bool _value)
		{
			Value = _value;
		}


		[SerializeField]
		protected bool m_value;
	}
}
