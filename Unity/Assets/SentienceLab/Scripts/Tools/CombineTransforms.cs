using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behaviour for checking several transforms and adapting the transformation of the first active transform.
/// This script can be used to combine, e.g., several optional hand controllers into one.
/// </summary>
/// 
public class CombineTransforms : MonoBehaviour
{
	public List<Transform> transforms;


	public void Update()
	{
		foreach (Transform t in transforms)
		{
			if ((t != null) && (t.gameObject.activeInHierarchy))
			{
				this.transform.position = t.position;
				this.transform.rotation = t.rotation;
				break;
			}
		}
	}
}
