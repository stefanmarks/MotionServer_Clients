using UnityEngine;
using MoCap;

/// <summary>
/// Component for scaling MoCap data selectively by bone/marker name.
/// </summary>
///
[AddComponentMenu("Motion Capture/Modifier/Selective Scale")]
[DisallowMultipleComponent]
public class SelectiveScaleModifier : MonoBehaviour, IModifier
{
	[Tooltip("Homogeneous scale factor.")]
	public float scaleFactor = 1.0f;

	[Tooltip("Prefix for any bone or marker name.")]
	public string namePrefix = "";

	[Tooltip("Names of bones or markers to selectively scale.")]
	public string[] names = { };


	public void Start()
	{
		// empty, but necessary to get the "Enable" button in the inspector
	}


	public void Process(ref MoCapData data)
	{
		if (!enabled) return;

		foreach (string name in names)
		{
			if (data.buffer.Name.Equals(namePrefix + name))
			{
				data.pos    *= scaleFactor;
				data.length *= scaleFactor;
				break;
			}
		}
		
	}
}
