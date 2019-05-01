using UnityEngine;

public class ApplicationQuitControl: MonoBehaviour
{
	public void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}
	}
}
