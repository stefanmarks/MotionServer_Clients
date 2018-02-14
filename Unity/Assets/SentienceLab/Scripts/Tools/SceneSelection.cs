using UnityEngine;
using UnityEngine.SceneManagement;
using SentienceLab.Input;
using SentienceLab.PostProcessing;
using System.Collections.Generic;

/// <summary>
/// Behaviour for selecting the next/previous scene in the build settings.
/// </summary>
/// 
public class SceneSelection : MonoBehaviour 
{
	[Tooltip("Action for tiggering the next scene")]
	public string actionNameNext = "nextScene";

	[Tooltip("Action for tiggering the previous scene")]
	public string actionNamePrev = "prevScene";

	[Tooltip("Duration in seconds to press the scene selection key")]
	public float activationTime = 1.0f;

	[Tooltip("Time in seconds for the fade out and in")]
	private float fadeTime = 1.0f;

	[Tooltip("Use keys 1-9 to directly select scenes")]
	public bool useNumberKeys = false;


	public void Start()
	{
		// get current scene number and maximum scene index
		sceneIndex        = SceneManager.GetActiveScene().buildIndex;
		currentSceneIndex = sceneIndex;
		maxSceneIndex     = SceneManager.sceneCountInBuildSettings - 1;

		// fade in
		fadeLevel = 1;
		fadeTime = -1;

		// create screen faders
		faders = ScreenFade.AttachToAllCameras();

		// create action handlers for next/prev scene selection
		actionNext = InputHandler.Find(actionNameNext);
		actionPrev = InputHandler.Find(actionNamePrev);
	}


	public void Update()
	{
		if (fadeLevel > 0)
		{
			// fade and level load is in progress

			fadeLevel += fadeTime * Time.deltaTime;
			if (fadeLevel < 0)
			{
				// fade in finished
				fadeLevel = 0;
				fadeTime = 0;
			}
			else if (fadeLevel > 1)
			{
				// fade to black finished -> load level
				fadeLevel = 1;
				fadeTime = 0;
				if (sceneIndex != currentSceneIndex)
				{
					Debug.Log("Loading scene " + sceneIndex);
					SceneManager.LoadScene(sceneIndex);
					sceneIndex = currentSceneIndex;
				}
			}

			foreach (ScreenFade fade in faders)
			{
				fade.FadeFactor = fadeLevel;
			}
		}
		else
		{
			// no fade in progress and no scene selected > check keys

			if (actionNext.IsActive())
			{
				if (timeout < activationTime)
				{
					// keep on pressing...
					timeout += Time.deltaTime;
				}
				else
				{
					// pressed long enough: select previous scene
					sceneIndex++;
				}
			}
			else if (actionPrev.IsActive())
			{
				if (timeout < activationTime)
				{
					// keep on pressing...
					timeout += Time.deltaTime;
				}
				else
				{
					// pressed long enough: select previous scene
					sceneIndex--;
				}
			}
			else
			{
				// buttons released
				timeout = 0;
			}

			if (useNumberKeys)
			{
				// check number keys
				for (int idx = 0; idx < Mathf.Min(maxSceneIndex, 9); idx++)
				{
					if (Input.GetKeyDown(KeyCode.Alpha1 + idx))
					{
						sceneIndex = idx;
					}
				}
			}

			// scene index sanity check
			if (sceneIndex < 0            ) { sceneIndex = maxSceneIndex; }
			if (sceneIndex > maxSceneIndex) { sceneIndex = 0;             }

			if (sceneIndex != currentSceneIndex)
			{
				// new scene number > start fading
				Debug.Log("About to load scene " + sceneIndex);

				// start the fade
				fadeLevel = 0.01f;
				fadeTime  = 1;
			}
		}
	}



	private float fadeLevel;
	private int   sceneIndex;
	private float timeout;
	private int   maxSceneIndex, currentSceneIndex;
	private List<ScreenFade> faders;
	private InputHandler     actionNext, actionPrev;
}
