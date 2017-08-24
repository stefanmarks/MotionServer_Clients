using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


[RequireComponent(typeof(Dropdown))]
public class OSC_DropdownVariable : MonoBehaviour, IOSCVariableContainer
{
	public string indexVariableName = "/dropdown1";
	public string labelVariableName = "/dropdownName1";

	public void Start()
	{
		dropdown = GetComponent<Dropdown>();
		dropdown.onValueChanged.AddListener(OnValueChanged);

		indexVar = new OSC_FloatVariable(indexVariableName, -0.5f, 1000);
		indexVar.DataReceivedEvent += OnReceivedOSC_Data;

		labelVar = new OSC_StringVariable(labelVariableName);

		updating = false;
	}


	protected void OnReceivedOSC_Data(OSC_Variable var)
	{
		if (!updating)
		{
			updating = true;
			if (indexVar.value < 0)
			{
				// value < 0: select previous item
				dropdown.value--;
			}
			else if ((indexVar.value > 0) && (indexVar.value < 0.9f))
			{
				// value >0 < 1: select next item
				dropdown.value++;
			}
			else if ( indexVar.value >= 1)
			{
				// value > 1: select item directly
				dropdown.value = (int)indexVar.value - 1;
			}

			labelVar.value = dropdown.options[dropdown.value].text;
			labelVar.SendUpdate();

			updating = false;
		}
	}


	protected void OnValueChanged(int value)
	{
		if (!updating)
		{
			updating = true;
			indexVar.value = dropdown.value;
			indexVar.SendUpdate();
			labelVar.value = dropdown.options[dropdown.value].text;
			labelVar.SendUpdate();
			updating = false;
		}
	}


	public void Update()
	{
		// nothing to do here
	}


	public List<OSC_Variable> GetOSC_Variables()
	{
		return new List<OSC_Variable>(new OSC_Variable[] { indexVar, labelVar });
	}


	private Dropdown           dropdown;
	private OSC_FloatVariable  indexVar;
	private OSC_StringVariable labelVar;
	private bool               updating;
}
