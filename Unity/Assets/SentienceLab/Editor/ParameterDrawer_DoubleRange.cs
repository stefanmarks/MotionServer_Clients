#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEditor;
using SentienceLab.Data;

[CustomPropertyDrawer(typeof(Parameter_DoubleRange.SValue))]

public class ParameterDrawer_DoubleRange : PropertyDrawer
{
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return base.GetPropertyHeight(property, label) + 16;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		SerializedProperty propLimitMin = property.FindPropertyRelative("limitMin");
		SerializedProperty propLimitMax = property.FindPropertyRelative("limitMax");
		SerializedProperty propValueMin = property.FindPropertyRelative("valueMin");
		SerializedProperty propValueMax = property.FindPropertyRelative("valueMax");

		float w = position.width  / 4f;
		float h = position.height / 2f;

		float newLimitMin = EditorGUI.FloatField(new Rect(position.x + 0 * w, position.y, w, h), (float) propLimitMin.doubleValue);
		float newValueMin = EditorGUI.FloatField(new Rect(position.x + 1 * w, position.y, w, h), (float) propValueMin.doubleValue);
		float newValueMax = EditorGUI.FloatField(new Rect(position.x + 2 * w, position.y, w, h), (float) propValueMax.doubleValue);
		float newLimitMax = EditorGUI.FloatField(new Rect(position.x + 3 * w, position.y, w, h), (float) propLimitMax.doubleValue);

		EditorGUI.MinMaxSlider(
			new Rect(position.x, position.y + h, position.width, h),
			ref newValueMin, ref newValueMax, newLimitMin, newLimitMax);

		propLimitMin.floatValue = newLimitMin;
		propValueMin.floatValue = newValueMin;
		propValueMax.floatValue = newValueMax;
		propLimitMax.floatValue = newLimitMax;
	}
}
