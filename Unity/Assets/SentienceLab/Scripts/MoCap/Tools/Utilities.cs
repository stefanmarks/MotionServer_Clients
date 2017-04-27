#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Utilities for Scenegraph queries and manipulations.
	/// </summary>
	/// 
	public static class Utilities
	{
		/// <summary>
		/// Finds a Transform with a specific name in a hierarchy starting at a specific Transform.
		/// </summary>
		/// <param name="name">name of the Transform to find</param>
		/// <param name="baseTransform">the Transform to start the search at</param>
		/// <returns>the Transform with the name or <code>null</code> if the Transform could not be found</returns>
		/// 
		static public Transform FindInHierarchy(string name, Transform baseTransform)
		{
			Transform result = null;

			if (baseTransform.name == name)
			{
				// it's the baseTransform itself
				result = baseTransform;
			}
			else
			{
				// let's look in all the children recursively
				foreach (Transform child in baseTransform)
				{
					result = Utilities.FindInHierarchy(name, child);
					if (result != null) break; // found it > get me out here
				}
			}

			return result;
		}
	}

}
