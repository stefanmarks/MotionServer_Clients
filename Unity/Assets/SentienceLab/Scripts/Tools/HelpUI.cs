#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using SentienceLab.Input;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class for managing a help UI that fades in when the specific object 
/// is held for a certain time within a collider in front of the camera.
/// </summary>
/// 
public class HelpUI : MonoBehaviour
{
	[Tooltip("Time in seconds before the help UI is shown")]
	public float  activateTime   = 1;

	[Tooltip("Time in seconds before the help UI is hidden")]
	public float  deactivateTime = 1;

	[Tooltip("Tag of colliders that trigger the UI")]
	public string triggerTag     = "help";

	[Tooltip("List of inputs that hide the UI when activated")]
	public string[] hideInputNames = { "trigger" };


	void Start()
	{
		canvas = GetComponentInChildren<Canvas>();
		canvas.enabled = true;
		time = deactivateTime;
		isWithinTrigger = false;

		hideActions = new List<InputHandler>();
		foreach (string action in hideInputNames)
		{
			hideActions.Add(InputHandler.Find(action));
		}
	}


	void Update()
	{
		// fade out UI when specific actions are active
		foreach (InputHandler handler in hideActions)
		{
			if (handler.IsActive())
			{
				isWithinTrigger = false;
				time = float.Epsilon;
			}
		}

		// check timing and show/hide UI accordingly
		if (isWithinTrigger && (time < activateTime))
		{
			time += Time.deltaTime;
			if (time >= activateTime)
			{
				canvas.enabled = true;
			}
		}
		else if (!isWithinTrigger && (time > 0))
		{
			time -= Time.deltaTime;
			if (time < 0)
			{
				canvas.enabled = false;
			}
		}
	}


	void OnTriggerEnter(Collider other)
	{
		if (other != null && other.tag.Equals(triggerTag))
		{
			isWithinTrigger = true;
			time = 0;
		}
	}


	void OnTriggerExit(Collider other)
	{
		if (other != null && other.tag.Equals(triggerTag))
		{
			isWithinTrigger = false;
			time = deactivateTime;
		}
	}


	private Canvas             canvas;
	private bool               isWithinTrigger;
	private float              time;
	private List<InputHandler> hideActions;
}
