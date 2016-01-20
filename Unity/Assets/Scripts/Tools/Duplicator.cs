using UnityEngine;

/// <summary>
/// Abstract base class for duplicating GameObjects into arrangements.
/// </summary>
/// 
public abstract class Duplicator : MonoBehaviour
{
	[Tooltip("The maximum delay of the clones in seconds.")]
	public float maximumDelay = 0;

	[Tooltip("The maximum additional random delay of the clones in seconds.")]
	public float randomDelayAmount = 0;


	// Use this for initialization
	void Start()
	{
		// create container element
		container = new GameObject();
		container.name = this.name + " Duplicates";
		container.transform.parent = this.transform.parent;

		// create initial copy...
		initialCopy = GameObject.Instantiate<GameObject>(this.gameObject);
		initialCopy.name = this.name + "_dup";
		initialCopy.transform.parent = container.transform;
		//...without any duplicators (recursion explosion)
		foreach (Duplicator duplicator in initialCopy.GetComponents<Duplicator>())
		{
			GameObject.Destroy(duplicator);
		}
		// set inactive so that no scripts are running and produce duplicate geometries
		initialCopy.SetActive(false);

		counter = 0;
	}
	

	/// <summary>
	/// Every frame, create a new copy until the desired amount is achieved.
	/// This is better than creating all copies at the beginning,
	/// causing a long startup delay.
	/// </summary>
	/// 
	void Update ()
	{
		if ( (initialCopy != null) && (counter < GetNumberOfCopies()) )
		{
			GameObject copy = GameObject.Instantiate<GameObject>(initialCopy);
			copy.name = initialCopy.name + "_" + counter;

			// generate parameter that goes from 0 to 1 and initialise delay variable
			float fParam = (float) counter / GetNumberOfCopies();
			float delay  = 0;
			
			// modify copy based on subclass
			ModifyDuplicate(copy, counter, fParam, out delay);

			// apply randomness
			delay *= maximumDelay;
			delay += Random.Range(0.0f, randomDelayAmount);

			// apply delay to clone (if applicable)
			MoCap.IDelay[] arrDelayable = copy.GetComponents<MoCap.IDelay>();
			foreach (MoCap.IDelay delayable in arrDelayable)
			{
				delayable.SetDelay(delay);
			}

			// add into scene
			copy.transform.parent = container.transform;
			copy.SetActive(true);

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


	private GameObject container;
	private GameObject initialCopy;
	private int        counter;
}
