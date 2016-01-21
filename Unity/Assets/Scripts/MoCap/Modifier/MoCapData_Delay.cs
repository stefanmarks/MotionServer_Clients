using UnityEngine;
using MoCap;
using System;

/// <summary>
/// Component for adding delay to MoCap data.
/// </summary>
///
[AddComponentMenu("Motion Capture/Data/Delay")]
[DisallowMultipleComponent]
public class MoCapData_Delay : MonoBehaviour, MoCapDataBuffer.Manipulator
{
	[Tooltip("Delay of the MoCap data in seconds.")]
	[Range(0.0f, 10.0f)]
	public float delay = 0;


	public void Process(ref MoCapDataBuffer.MoCapData data)
	{
		// The actual delay happens in the MoCapDataBuffer class
		// by storing the data in a FIFO the length of which
		// is determined by the "delay" value of this component
	}
}
