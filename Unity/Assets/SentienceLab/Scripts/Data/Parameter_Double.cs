#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using UnityEngine;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Double")]
	public class Parameter_Double : ParameterBase, IParameterModify, IParameterAsBoolean
	{
		public delegate void LimitChanged(ParameterBase _value);
		public event LimitChanged OnLimitChanged;


		[Serializable]
		public struct SValue
		{
			public double limitMin;
			public double limitMax;
			public double value;
		}

		[SerializeField]
		private SValue value;



		public new void Start()
		{
			base.Start();
			// initialise old value structure with values that will force updates
			m_oldValue.value    = double.MaxValue;
			m_oldValue.limitMax = double.MaxValue;
			m_oldValue.limitMin = double.MinValue;
		}


		/// <summary>
		/// Sets the limits.
		/// </summary>
		///
		public void SetLimits(double _min, double _max)
		{
			value.limitMin = System.Math.Min(_min, _max);
			value.limitMax = System.Math.Max(_min, _max);
			value.value    = System.Math.Min(value.limitMax, System.Math.Max(value.limitMin, value.value));
			m_checkForChange = true;
		}

		/// <summary>
		/// The minimum possible value
		/// </summary>
		public double LimitMin
		{
			get { return value.limitMin; }
		}

		/// <summary>
		/// The maximum possible value
		/// </summary>
		public double LimitMax
		{
			get { return value.limitMax; }
		}

		/// <summary>
		/// The actual value.
		/// </summary>
		///
		public double Value
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
		public void IncludeInLimits(double _value)
		{
			this.value.limitMin = System.Math.Min(this.value.limitMin, _value);
			this.value.limitMax = System.Math.Max(this.value.limitMax, _value);

			m_checkForChange = true;
		}


		/// <summary>
		/// Maps a value between min/max limit to a range of [0...1].
		/// </summary>
		/// <param name="_value">the value to map</param>
		/// <returns>the mapped value in a range from [0...1]</returns>
		///
		public double MapTo01(double _value)
		{
			return (_value - this.value.limitMin) / (this.value.limitMax - this.value.limitMin);
		}

		/// <summary>
		/// Maps a value between min/max limit to a range of [0...MaxInteger].
		/// </summary>
		/// <param name="_value">the value to map</param>
		/// <returns>the mapped value in a range from [0...Max_Integer]</returns>
		///
		public int MapToInt(double _value)
		{
			return (int)((_value - this.value.limitMin) / (this.value.limitMax - this.value.limitMin) * int.MaxValue);
		}

		/// <summary>
		/// Maps a value between [0...1] to the min/max range.
		/// </summary>
		/// <param name="_value">the value to map</param>
		/// <returns>the mapped value in a range from [min...max]</returns>
		///
		public double MapFrom01(double _value)
		{
			return (_value * (this.value.limitMax - this.value.limitMin)) + this.value.limitMin;
		}


		/// <summary>
		/// Creates a formatted string.
		/// The value and limits from/to are passed to the formatting function as parameters #0, 1, 2.
		/// If the parameter _formatString contains a "y", the elements #3, 4, 5 are of type DateTime.
		/// </summary>
		/// <param name="_formatString">the format string to use</param>
		/// <returns>the formatted string</returns>
		///
		public override string ToFormattedString(string _formatString)
		{
			if (_formatString.Contains("y"))
			{
				// special case Year
				return string.Format(_formatString, 
					value.value, value.limitMin, value.limitMax,
					DateTime.FromOADate(value.value), DateTime.FromOADate(value.limitMin), DateTime.FromOADate(value.limitMax));
			}
			else
			{
				return string.Format(_formatString, value.value, value.limitMin, value.limitMax);
			}
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
			Value += _delta;
		}


		public override string ToString()
		{
			return Name + ":Double:" + value.limitMin + " [" + value.value + "] " + value.limitMax;
		}


		protected SValue m_oldValue;
	}
}
