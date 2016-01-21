using UnityEngine;
using MoCap;
using System;

/// <summary>
/// Component for scaline MoCap data.
/// </summary>
///
[AddComponentMenu("Motion Capture/Data/Scale")]
[DisallowMultipleComponent]
public class MoCapData_Scale : MonoBehaviour, MoCapDataBuffer.Manipulator
{
	[Tooltip("Homogeneous scale factor.")]
	public float scaleFactor = 1.0f;


	public void Process(ref MoCapDataBuffer.MoCapData data)
	{
		data.pos    *= scaleFactor;
		data.length *= scaleFactor;
	}
}
