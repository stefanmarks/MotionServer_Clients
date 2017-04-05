using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Utility class for class and interface operations.
	/// </summary>
	/// 
	public class ClassUtils
	{
		/// <summary>
		/// Finds all interfaces of a specific kind in the scene. 
		/// </summary>
		/// <typeparam name="T">Class or interface type to find</typeparam>
		/// <returns>List of classes of the type</returns>
		/// 
		public static ICollection<T> FindAll<T>()
		{
			List<T> interfaceList = new List<T>();

			// get list of all root objects and then search for the requested type in each hierarchy
			GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
			foreach (GameObject comp in rootObjects)
			{
				interfaceList.AddRange(comp.GetComponentsInChildren<T>());
			}

			return interfaceList;
		}
	}
}
