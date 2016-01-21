using UnityEngine;
using MoCap;

/// <summary>
/// Component for adding random noise to MoCap data.
/// </summary>
///
[AddComponentMenu("Motion Capture/Data/Randomize")]
public class MoCapData_Randomize : MonoBehaviour, MoCapDataBuffer.Manipulator
{
	public enum Influence
	{
		Position, Rotation
	}

	[Tooltip("Mode of influence.")]
	public Influence influence = Influence.Position;

	[Tooltip("Amount of randomness.")]
	public float amount = 0;


	public void Process(ref MoCapDataBuffer.MoCapData data)
	{
		switch (influence)
		{
			case Influence.Position:
				data.pos.x += amount * Mathf.PerlinNoise(data.pos.y, data.pos.z);
				data.pos.y += amount * Mathf.PerlinNoise(data.pos.x, data.pos.z);
				data.pos.z += amount * Mathf.PerlinNoise(data.pos.x, data.pos.y);
				break;

			
		}
	}
}
