#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab.Data
{
	[AddComponentMenu("Parameter/List")]
	public class Parameter_List : ParameterBase, IParameterModify
	{
		[Serializable]
		public struct SItem
		{
			public string text;
			public Sprite image;
			public object data;

			public SItem(string _text, Sprite _image = null, object _data = null)
			{
				text = _text; image = _image; data = _data;
			}
		}


		public delegate void ListChanged(Parameter_List _list);
		public event ListChanged OnListChanged;


		[SerializeField]
		protected List<SItem> listItems = new List<SItem>();


		public int Count { get { return listItems.Count; } }


		public new void Start()
		{
			base.Start();
			// initialise old value structure with values that will force updates
			m_selectedItem    = -1;
			m_oldSelectedItem = -1;
			m_floatDelta      = 0;
		}


		public void Clear()
		{
			listItems.Clear();
			m_selectedItem = -1;
			CheckForChange();
		}


		/// <summary>
		/// Adds an item.
		/// </summary>
		///
		public void AddItem(SItem item)
		{
			listItems.Add(item);
			if (OnListChanged != null) OnListChanged.Invoke(this);

			if (m_selectedItem < 0)
			{
				m_selectedItem = 0;
				CheckForChange();
			}
		}


		public SItem GetItem(int _index)
		{
			return ((_index < 0) || (_index >= listItems.Count)) ? NULL_ITEM : listItems[_index];
		}


		public int SelectedItemIndex
		{
			get
			{
				return m_selectedItem;
			}

			set
			{
				m_selectedItem = SanityCheck(value);
				CheckForChange();
			}
		}


		public SItem SelectedItem
		{
			get
			{
				return (m_selectedItem < 0) ? NULL_ITEM : listItems[m_selectedItem];
			}
		}



		/// <summary>
		/// Creates a formatted string.
		/// The selected tem text and index are passed to the formatting function as parameters #0, 1.
		/// </summary>
		/// <param name="_formatString">the format string to use</param>
		/// <returns>the formatted string</returns>
		///
		public override string ToFormattedString(string _formatString)
		{
			return string.Format(_formatString, SelectedItem.text, m_selectedItem);
		}


		protected int SanityCheck(int _index)
		{
			if (_index < 0) { _index = 0; }
			if (_index >= listItems.Count) { _index = listItems.Count - 1; }
			return _index;
		}


		/// <summary>
		/// Check for changes to limits of the value and call event handlers accordingly.
		/// </summary>
		/// 
		protected override void CheckForChange()
		{
			if (m_oldSelectedItem != m_selectedItem)
			{
				m_oldSelectedItem = m_selectedItem;
				InvokeOnValueChanged();
			}
		}


		public void ChangeValue(float _delta, int _idx = 0)
		{
			m_floatDelta += _delta;
			int intDelta = (int)Math.Abs(m_floatDelta);
			if (intDelta > 0)
			{
				m_selectedItem += (m_floatDelta < 0) ? -intDelta : intDelta;
				m_floatDelta   -= (m_floatDelta < 0) ? -intDelta : intDelta;
			}
			m_selectedItem = SanityCheck(m_selectedItem);
			CheckForChange();
		}


		public override string ToString()
		{
			return Name + ":List[" + m_selectedItem + "]=\"" + SelectedItem.text + "\"";
		}


		protected int   m_selectedItem;
		protected int   m_oldSelectedItem;
		protected float m_floatDelta;

		static protected SItem NULL_ITEM = new SItem("");
	}
}