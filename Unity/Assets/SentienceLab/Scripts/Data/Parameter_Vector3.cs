#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using UnityEngine;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/Vector3")]
	public class Parameter_Vector3 : ParameterBase
	{
		public delegate void LimitChanged(ParameterBase _value);
		public event LimitChanged OnLimitChanged;


		[Serializable]
		public struct SValue
		{
			public Vector3 limitMin;
			public Vector3 limitMax;
			public Vector3 value;
		}

		[SerializeField]
		private SValue value;



		public new void Start()
		{
			base.Start();
			// initialise old value structure with values that will force updates
			m_oldValue.value    = float.MaxValue * Vector3.one;
			m_oldValue.limitMax = float.MaxValue * Vector3.one;
			m_oldValue.limitMin = float.MinValue * Vector3.one;
		}


		/// <summary>
		/// Sets the limits.
		/// </summary>
		///
		public void SetLimits(Vector3 _min, Vector3 _max)
		{
			value.limitMin = Vector3.Min(_min, _max);
			value.limitMax = Vector3.Max(_min, _max);
			value.value    = Vector3.Min(value.limitMax, Vector3.Max(value.limitMin, value.value));
			m_checkForChange = true;
		}

		/// <summary>
		/// The minimum possible value
		/// </summary>
		public Vector3 LimitMin
		{
			get { return value.limitMin; }
		}

		/// <summary>
		/// The maximum possible value
		/// </summary>
		public Vector3 LimitMax
		{
			get { return value.limitMax; }
		}

		/// <summary>
		/// The actual value.
		/// </summary>
		///
		public Vector3 Value
		{
			get { return value.value; }
			set
			{
				this.value.value = Vector3.Min(Vector3.Max(this.value.limitMin, value), this.value.limitMax);
				m_checkForChange = true;
			}
		}


		/// <summary>
		/// Includes a value in the limits by extending those if necessary.
		/// </summary>
		/// <param name="_value">the value to include in the limits</param>
		///
		public void IncludeInLimits(Vector3 _value)
		{
			this.value.limitMin = Vector3.Min(this.value.limitMin, _value);
			this.value.limitMax = Vector3.Max(this.value.limitMax, _value);

			m_checkForChange = true;
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


		/// <summary>
		/// Maps x/y/z vector values between min/max limits to a range of [0...1].
		/// </summary>
		/// <param name="_value">the x/y/z values to map</param>
		/// <returns>the mapped values in a range from [0...1]</returns>
		///
		public Vector3 MapTo01(Vector3 _value)
		{
			_value.x = (_value.x - this.value.limitMin.x) / (this.value.limitMax.x - this.value.limitMin.x);
			_value.y = (_value.y - this.value.limitMin.y) / (this.value.limitMax.y - this.value.limitMin.y);
			_value.z = (_value.z - this.value.limitMin.z) / (this.value.limitMax.z - this.value.limitMin.z);
			return _value;
		}


		/// <summary>
		/// Maps x/y/z vector values between [0...1] to the min/max range.
		/// </summary>
		/// <param name="_value">the x/y/z values to map</param>
		/// <returns>the mapped values in a range from [min...max]</returns>
		///
		public Vector3 MapFrom01(Vector3 _value)
		{
			_value.x = (_value.x * (this.value.limitMax.x - this.value.limitMin.x)) + this.value.limitMin.x;
			_value.y = (_value.y * (this.value.limitMax.y - this.value.limitMin.y)) + this.value.limitMin.y;
			_value.z = (_value.z * (this.value.limitMax.z - this.value.limitMin.z)) + this.value.limitMin.z;
			return _value;
		}


		/// <summary>
		/// Creates a formatted string.
		/// The X/Y/Z values of the vector are passed to the formatting function as parameters #0, 1, 2.
		/// The X/Y/Z values of the minimum limits are passed to the formatting function as parameters #3, 4, 5.
		/// The X/Y/Z values of the maximum limits are passed to the formatting function as parameters #6, 7, 8.
		/// 
		/// </summary>
		/// <param name="_formatString">the format string to use</param>
		/// <returns>the formatted string</returns>
		///
		public override string ToFormattedString(string _formatString)
		{
			return string.Format(_formatString, 
				value.value.x, value.value.y, value.value.z,
				value.limitMin.x, value.limitMin.y, value.limitMin.z,
				value.limitMax.x, value.limitMax.y, value.limitMax.z
			);
		}


		public override string ToString()
		{
			return Name + ":Vector3:" + value.limitMin + " [" + value.value + "] " + value.limitMax;
		}


		protected SValue m_oldValue;
	}
}
