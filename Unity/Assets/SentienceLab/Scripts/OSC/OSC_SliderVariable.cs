using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


[RequireComponent(typeof(Slider))]
public class OSC_SliderVariable : MonoBehaviour, IOSCVariableContainer
{
	public string variableName = "/slider1";


	public void Start()
	{
		slider = GetComponent<Slider>();
		slider.onValueChanged.AddListener(delegate { OnSliderChanged(); });

		variable       = new OSC_FloatVariable();
		variable.name  = variableName;
		variable.min   = slider.minValue;
		variable.max   = slider.maxValue;
		variable.value = slider.value;

		variable.DataReceivedEvent += OnReceivedOSC_Data;

		updating = false;
	}


	protected void OnReceivedOSC_Data(OSC_Variable var)
	{
		if (!updating)
		{
			updating = true;
			slider.value = variable.value;
			updating = false;
		}
	}


	protected void OnSliderChanged()
	{
		if (!updating)
		{
			updating = true;
			variable.value = slider.value;
			variable.SendUpdate();
			updating = false;
		}
	}


	public void Update()
	{
		// nothing to do here
	}


	public List<OSC_Variable> GetOSC_Variables()
	{
		return new List<OSC_Variable>(new OSC_Variable[] { variable });
	}


	private Slider            slider;
	private OSC_FloatVariable variable;
	private bool              updating;
}
