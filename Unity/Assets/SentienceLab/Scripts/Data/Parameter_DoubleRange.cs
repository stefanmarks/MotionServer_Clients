#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using UnityEngine;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Double Range")]
	public class Parameter_DoubleRange : ParameterBase, IParameterModify
	{
		public delegate void LimitChanged(ParameterBase _value);
		public event LimitChanged OnLimitChanged;


		[Serializable]
		public struct SValue
		{
			public double valueMin;
			public double valueMax;
			public double limitMin;
			public double limitMax;
		}

		[SerializeField]
		private SValue value;



		public new void Start()
		{
			base.Start();
			// initialise old value structure with values that will force updates
			m_oldValue.valueMin = double.MaxValue;
			m_oldValue.valueMax = double.MinValue;
			m_oldValue.limitMax = double.MaxValue;
			m_oldValue.limitMin = double.MinValue;
		}


		/// <summary>
		/// Sets the limits.
		/// </summary>
		///
		public void SetLimits(double min, double max)
		{
			value.limitMin = System.Math.Min(min, max);
			value.limitMax = System.Math.Max(min, max);
			value.valueMin = System.Math.Max(value.limitMin, value.valueMin);
			value.valueMax = System.Math.Min(value.limitMax, value.valueMax);
			m_checkForChange = true;
		}


		/// <summary>
		/// Sets the range to the limits.
		/// </summary>
		///
		public void SetMaximumRange()
		{
			value.valueMin = value.limitMin;
			value.valueMax = value.limitMax;
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
		/// The start value.
		/// </summary>
		///
		public double ValueMin
		{
			get { return value.valueMin; }
			set
			{
				this.value.valueMin = System.Math.Max(System.Math.Min(LimitMax, value), LimitMin);
				this.value.valueMax = System.Math.Max(this.value.valueMax, this.value.valueMin);
				m_checkForChange = true;
			}
		}

		/// <summary>
		/// The end value.
		/// </summary>
		public double ValueMax
		{
			get { return value.valueMax; }
			set
			{
				this.value.valueMax = System.Math.Min(System.Math.Max(LimitMin, value), LimitMax);
				this.value.valueMin = System.Math.Min(this.value.valueMax, this.value.valueMin);
				m_checkForChange = true;
			}
		}


		/// <summary>
		/// Includes a value in the limits by extending those if necessary.
		/// </summary>
		/// <param name="value">the value to include in the limits</param>
		///
		public void IncludeInLimits(double value)
		{
			this.value.limitMin = System.Math.Min(this.value.limitMin, value);
			this.value.limitMax = System.Math.Max(this.value.limitMax, value);

			m_checkForChange = true;
		}

		/// <summary>
		/// Includes a value in the value range by extending the from/to values if necessary.
		/// </summary>
		/// <param name="value">the value to include in the value range</param>
		///
		public void IncludeInRange(double value)
		{
			this.value.valueMin = System.Math.Max(this.value.limitMin, System.Math.Min(this.value.valueMin, value));
			this.value.valueMax = System.Math.Min(this.value.limitMax, System.Math.Max(this.value.valueMax, value));

			m_checkForChange = true;
		}

		/// <summary>
		/// Maps a value between min/max limit to a range of [0...1].
		/// </summary>
		/// <param name="value">the value to map</param>
		/// <returns>the mapped value in a range from [0...1]</returns>
		///
		public double MapTo01(double value)
		{
			return ((value - this.value.limitMin) / (this.value.limitMax - this.value.limitMin));
		}

		/// <summary>
		/// Maps a value between min/max limit to a range of [0...MaxInteger].
		/// </summary>
		/// <param name="value">the value to map</param>
		/// <returns>the mapped value in a range from [0...Max_Integer]</returns>
		///
		public int MapToInt(double value)
		{
			return (int)((value - this.value.limitMin) / (this.value.limitMax - this.value.limitMin) * int.MaxValue);
		}

		/// <summary>
		/// Maps a value between [0...1] to the min/max range.
		/// </summary>
		/// <param name="value">the value to map</param>
		/// <returns>the mapped value in a range from [min...max]</returns>
		///
		public double MapFrom01(double value)
		{
			return (value * (this.value.limitMax - this.value.limitMin)) + this.value.limitMin;
		}


		/// <summary>
		/// Creates a formatted string.
		/// The value from/to and limits from/to are passed to the formatting function as parameters #0, 1, 2, 3.
		/// If the parameter _formatString contains a "y", the elements #4, 5, 6, 7 are of type DateTime.
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
					value.valueMin, value.valueMax, value.limitMin, value.limitMax,
					DateTime.FromOADate(value.valueMin), DateTime.FromOADate(value.valueMax), DateTime.FromOADate(value.limitMin), DateTime.FromOADate(value.limitMax));
			}
			else
			{
				return string.Format(_formatString, value.valueMin, value.valueMax, value.limitMin, value.limitMax);
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
			if ((m_oldValue.valueMin != value.valueMin) || (m_oldValue.valueMax != value.valueMax))
			{
				m_oldValue.valueMin = value.valueMin;
				m_oldValue.valueMax = value.valueMax;
				InvokeOnValueChanged();
			}
		}


		public void ChangeValue(float _delta, int _idx = 0)
		{
			if (_idx == 1)
			{
				ValueMax += _delta;
			}
			else
			{
				ValueMin += _delta;
			}
		}


		public override string ToString()
		{
			return Name + ":DoubleRange:" + value.limitMin + " [" + value.valueMin + " ... " + value.valueMax + "] " + value.limitMax;
		}


		protected SValue m_oldValue;
	}
}