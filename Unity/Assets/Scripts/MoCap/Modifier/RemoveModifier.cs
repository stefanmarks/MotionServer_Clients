using UnityEngine;

namespace MoCap
{
	/// <summary>
	/// Component for scaline MoCap data.
	/// </summary>
	///

	[AddComponentMenu("Motion Capture/Modifier/Remove")]
	[DisallowMultipleComponent]

	public class RemoveModifier : MonoBehaviour, IModifier
	{
		[Tooltip("Prefix for any bone or marker name.")]
		public string namePrefix = "";

		[Tooltip("Names of bones or markers to always disable.")]
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
					data.tracked = false;
					break;
				}
			}
		}
	}
}
