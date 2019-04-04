#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEditor;
using SentienceLab.Data;

[CustomPropertyDrawer(typeof(Parameter_Integer.SValue))]

public class ParameterDrawer_Integer : PropertyDrawer
{
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return base.GetPropertyHeight(property, label) + 16;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		SerializedProperty propMin   = property.FindPropertyRelative("limitMin");
		SerializedProperty propMax   = property.FindPropertyRelative("limitMax");
		SerializedProperty propValue = property.FindPropertyRelative("value");

		float w = position.width  / 2f;
		float h = position.height / 2f;
		float labelW = 32;
		EditorGUI.LabelField(new Rect(position.x + 0 * w, position.y, labelW, h), "Min:");
		long newMin = EditorGUI.LongField(new Rect(position.x + 0 * w + labelW, position.y, w - labelW, h), propMin.longValue);
		EditorGUI.LabelField(new Rect(position.x + 1 * w, position.y, labelW, h), "Max:");
		long newMax = EditorGUI.LongField(new Rect(position.x + 1 * w + labelW, position.y, w - labelW, h), propMax.longValue);

		long newValue = (long) EditorGUI.Slider(
			new Rect(position.x, position.y + h, position.width, h),
			(float) propValue.longValue, newMin, newMax);

		propMin.longValue   = newMin;
		propValue.longValue = newValue;
		propMax.longValue   = newMax;
	}
}
