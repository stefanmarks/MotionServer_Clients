#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using UnityEngine;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Integer")]
	public class Parameter_Integer : ParameterBase, IParameterModify, IParameterAsBoolean
	{
		public delegate void LimitChanged(ParameterBase _value);
		public event LimitChanged OnLimitChanged;


		[Serializable]
		public struct SValue
		{
			public long limitMin;
			public long limitMax;
			public long value;
		}

		[SerializeField]
		private SValue value;



		public new void Start()
		{
			base.Start();
			// initialise old value structure with values that will force updates
			m_oldValue.value    = long.MaxValue;
			m_oldValue.limitMax = long.MaxValue;
			m_oldValue.limitMin = long.MinValue;
			m_delta = 0;
		}


		/// <summary>
		/// Sets the limits.
		/// </summary>
		///
		public void SetLimits(long _min, long _max)
		{
			value.limitMin = System.Math.Min(_min, _max);
			value.limitMax = System.Math.Max(_min, _max);
			value.value    = System.Math.Min(value.limitMax, System.Math.Max(value.limitMin, value.value));
			m_checkForChange = true;
		}

		/// <summary>
		/// The minimum possible value
		/// </summary>
		public long LimitMin
		{
			get { return value.limitMin; }
		}

		/// <summary>
		/// The maximum possible value
		/// </summary>
		public long LimitMax
		{
			get { return value.limitMax; }
		}

		/// <summary>
		/// The actual value.
		/// </summary>
		///
		public long Value
		{
			get { return value.value; }
			set
			{
				this.value.value = System.Math.Min(System.Math.Max(this.value.limitMin, value), this.value.limitMax);
				m_checkForChange = true;
			}
		}


		/// <summary>
		/// Includes a value in the limits by extending those if necessary.
		/// </summary>
		/// <param name="_value">the value to include in the limits</param>
		///
		public void IncludeInLimits(long _value)
		{
			this.value.limitMin = System.Math.Min(this.value.limitMin, _value);
			this.value.limitMax = System.Math.Max(this.value.limitMax, _value);

			m_checkForChange = true;
		}


		/// <summary>
		/// Creates a formatted string.
		/// The value and limits from/to are passed to the formatting function as parameters #0, 1, 2.
		/// </summary>
		/// <param name="_formatString">the format string to use</param>
		/// <returns>the formatted string</returns>
		///
		public override string ToFormattedString(string _formatString)
		{
			return string.Format(_formatString, value.value, value.limitMin, value.limitMax);
		}


		/// <summary>
		/// Check for changes to limits of the value and call event handlers accordingly.
		/// </summary>
		/// 
		protected override void CheckForChange()
		{
			if ((m_oldValue.limitMin != value.limitMin) || (m_oldValue.limitMax != value.limitMax))
			{
				m_oldValue.limitMin = value.limitMin;
				m_oldValue.limitMax = value.limitMax;
				if (OnLimitChanged != null) OnLimitChanged.Invoke(this);
			}
			if (m_oldValue.value != value.value)
			{
				InvokeOnValueChanged();
				m_oldValue.value = value.value;
			}
		}


		public bool GetBooleanValue()
		{
			// threshold for bool value is the middle limit
			return Value > ((value.limitMin + value.limitMax) / 2);
		}


		public void SetBooleanValue(bool _value)
		{
			Value = _value ? value.limitMax : value.limitMin;
		}


		public void ChangeValue(float _delta, int _idx)
		{
			// remember fractional changes to an integer
			m_delta += _delta;
			if (m_delta >= 1.0)
			{
				long delta = (long)m_delta;
				Value   += delta;
				m_delta -= delta;
			}
			else if (m_delta <= -1.0)
			{
				long delta = (long)(-m_delta);
				Value   -= delta;
				m_delta += delta;
			}
		}


		public override string ToString()
		{
			return Name + ":Integer:" + value.limitMin + " [" + value.value + "] " + value.limitMax;
		}


		protected SValue m_oldValue;
		protected double m_delta;
	}
}