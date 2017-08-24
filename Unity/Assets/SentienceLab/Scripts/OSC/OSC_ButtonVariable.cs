using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


[RequireComponent(typeof(Button))]
public class OSC_ButtonVariable : MonoBehaviour, IOSCVariableContainer
{
	public string variableName = "/button1";


	public void Start()
	{
		button = GetComponent<Button>();
		button.onClick.AddListener(OnButtonClicked);

		variable = new OSC_BoolVariable(variableName);
		variable.DataReceivedEvent += OnReceivedOSC_Data;

		updating = false;
	}


	protected void OnReceivedOSC_Data(OSC_Variable var)
	{
		if (!updating)
		{
			updating = true;
			if (variable.value)
			{
				button.onClick.Invoke();
			}
			updating = false;
		}
	}


	protected void OnButtonClicked()
	{
		if (!updating)
		{
			updating = true;
			variable.value = true;
			variable.SendUpdate();
			variable.value = false;
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


	private Button           button;
	private OSC_BoolVariable variable;
	private bool             updating;
}
