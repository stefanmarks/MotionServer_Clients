#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

/// <summary>
/// Makes one tranform follow the movement of another
/// </summary>
/// 
public class FollowTransform : MonoBehaviour
{
	[Tooltip("The transform to follow")]
	public Transform sourceTransform;

	public void Start ()
	{
		// nothing to do here
	}


	public void Update()
	{
		CopyTransform();
	}


	public void LateUpdate()
	{
		CopyTransform();
	}


	private void CopyTransform()
	{
		this.transform.position = sourceTransform.position;
		this.transform.rotation = sourceTransform.rotation;
	}
}
