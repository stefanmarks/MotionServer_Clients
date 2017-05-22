#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Component for adding random noise to MoCap data.
	/// </summary>
	///

	[AddComponentMenu("Motion Capture/Modifier/Randomize")]

	public class RandomizeModifier : MonoBehaviour, IMoCapDataModifier
	{
		public enum Influence
		{
			Position, Rotation
		}

		[Tooltip("Mode of influence.")]
		public Influence influence = Influence.Position;

		[Tooltip("Amount of randomness.")]
		public float amount = 0;


		public void Start()
		{
			// empty, but necessary to get the "Enable" button in the inspector
		}


		public void Process(ref MoCapData data)
		{
			if (!enabled) return;
			switch (influence)
			{
				case Influence.Position: // TODO: Not very nice implementation so far
					data.pos.x += amount * Mathf.PerlinNoise(data.pos.y, data.pos.z);
					data.pos.y += amount * Mathf.PerlinNoise(data.pos.x, data.pos.z);
					data.pos.z += amount * Mathf.PerlinNoise(data.pos.x, data.pos.y);
					break;
			}
		}

		public int GetRequiredBufferSize()
		{
			return 1;
		}
	}

}
