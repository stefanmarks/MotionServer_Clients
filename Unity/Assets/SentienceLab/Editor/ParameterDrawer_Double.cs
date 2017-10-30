using UnityEngine;
using UnityEditor;
using SentienceLab.Data;

[CustomPropertyDrawer(typeof(Parameter_Double.SValue))]

public class ParameterDrawer_Double : PropertyDrawer
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
		float newMin = EditorGUI.FloatField(new Rect(position.x + 0 * w + labelW, position.y, w - labelW, h), (float) propMin.doubleValue);
		EditorGUI.LabelField(new Rect(position.x + 1 * w, position.y, labelW, h), "Max:");
		float newMax = EditorGUI.FloatField(new Rect(position.x + 1 * w + labelW, position.y, w - labelW, h), (float) propMax.doubleValue);

		float newValue = EditorGUI.Slider(
			new Rect(position.x, position.y + h, position.width, h),
			propValue.floatValue, newMin, newMax);

		propMin.floatValue   = newMin;
		propValue.floatValue = newValue;
		propMax.floatValue   = newMax;
	}
}
