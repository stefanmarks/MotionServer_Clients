using UnityEngine;
using MoCap;
using System;

/// <summary>
/// Component for mirroring MoCap data along a principal axis.
/// </summary>
///
[AddComponentMenu("Motion Capture/Data/Mirror")]
[DisallowMultipleComponent]
public class MoCapData_Mirror : MonoBehaviour, MoCapDataBuffer.Manipulator
{
	public enum Axis
	{
		X_Axis, Y_Axis, Z_Axis
	}

	[Tooltip("Axis to mirror data along.")]
	public Axis axis = Axis.Z_Axis;


	public void Process(ref MoCapDataBuffer.MoCapData data)
	{
		switch (axis)
		{
			case Axis.X_Axis:
				data.pos.x = -data.pos.x;
				data.rot.x = -data.rot.x;
				data.rot.w = -data.rot.w;
				break;

			case Axis.Y_Axis:
				data.pos.y = -data.pos.y;
				//data.rot.y = -data.rot.y;
				//data.rot.w = -data.rot.w;
				break;

			case Axis.Z_Axis:
				data.pos.z = -data.pos.z;
				data.rot.z = -data.rot.z;
				data.rot.w = -data.rot.w;
				break;
		}
	}
}

