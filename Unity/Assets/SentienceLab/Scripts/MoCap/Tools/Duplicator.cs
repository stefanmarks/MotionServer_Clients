#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Abstract base class for duplicating GameObjects into arrangements.
	/// </summary>
	///
	public abstract class Duplicator : MonoBehaviour
	{
		[Tooltip("The GameObject to duplicate.")]
		public GameObject template;

		[Tooltip("The minimum delay of the clones in seconds.")]
		public float minimumDelay = 0;

		[Tooltip("The maximum delay of the clones in seconds.")]
		public float maximumDelay = 0;

		[Tooltip("The maximum additional random delay of the clones in seconds.")]
		public float randomDelayAmount = 0;


		// Use this for initialization
		void Start()
		{
			if (template != null)
			{
				// deactivate to make sure the original doesn't show
				template.SetActive(false);
			}
			counter = 0;
		}


		/// <summary>
		/// Every frame, create a new copy until the desired amount is achieved.
		/// This is better than creating all copies at the beginning,
		/// causing a long startup delay.
		/// </summary>
		/// 
		void Update()
		{
			if ((template != null) && (counter < GetNumberOfCopies()))
			{
				GameObject copy = GameObject.Instantiate<GameObject>(template);
				copy.name = template.name + "_" + counter;
				copy.transform.parent = this.transform;
				copy.SetActive(true);

				// generate parameter that goes from 0 to 1 and initialise delay variable
				float fParam = (float) counter / GetNumberOfCopies();
				float delay = 0;

				// modify copy based on subclass
				ModifyDuplicate(copy, counter, fParam, out delay);

				// apply randomness
				delay = minimumDelay + (maximumDelay - minimumDelay) * delay;
				delay += Random.Range(0.0f, randomDelayAmount);

				// apply delay to clone (if applicable)
				DelayModifier[] arrDelayable = copy.GetComponents<DelayModifier>();
				if ((arrDelayable.Length == 0) && (delay > 0))
				{
					// no delay modifier > add one (if necessary)
					copy.AddComponent<DelayModifier>();
					arrDelayable = copy.GetComponents<DelayModifier>();
				}
				foreach (DelayModifier delayable in arrDelayable)
				{
					delayable.delay   = delay;
					delayable.enabled = true;
				}

				counter++;
			}
		}


		/// <summary>
		/// Modifies the duplicate to arrange and configure it accordingly to the subclass behaviour.
		/// </summary>
		/// <param name="copy">the GameObject to modify</param>
		/// <param name="counter">what stage is the counter at</param>
		/// <param name="fParameter">parameter between [0...1) based on the counter</param>
		/// <param name="delay">delay value between [0 and 1] to apply to the copy</param>
		/// 
		public abstract void ModifyDuplicate(GameObject copy, int counter, float fParameter, out float delay);


		/// <summary>
		/// Returns the total number of copies to make.
		/// </summary>
		/// <returns>the number of total copies to make</returns>
		/// 
		protected abstract int GetNumberOfCopies();


		private int counter;
	}

}
