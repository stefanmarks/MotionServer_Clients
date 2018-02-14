#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab.PostProcessing
{
	/// <summary>
	/// Class for fading the screen using postprocessing.
	/// </summary>

	[DisallowMultipleComponent]
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("Effects/Screen Fade")]

	public class ScreenFade : MonoBehaviour
	{
		[Tooltip("The colour to fade to")]
		public Color FadeColour = Color.black;

		[Tooltip("The fade factor (0: no fade, 1: completely faded)")]
		[Range(0, 1)]
		public float FadeFactor = 0;


		public void Awake()
		{
			Shader shader = Shader.Find("FX/Screen Fade");
			if (shader != null)
			{
				m_FadeMaterial = new Material(shader);
			}
			else
			{
				Debug.LogWarning("Shader 'FX/Screen Fade' not found. Is it included in the list of preloaded shaders or in a 'Resources' folder?");
			}
		}


		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (m_FadeMaterial != null)
			{
				m_FadeMaterial.SetColor("_Colour", FadeColour);
				m_FadeMaterial.SetFloat("_Fade", FadeFactor);
				Graphics.Blit(source, destination, m_FadeMaterial);
			}
			else
			{
				// fallback: show at least the original image
				Graphics.Blit(source, destination);
			}
		}


		/// <summary>
		/// Attached the fade effect to all cameras and returns a list to the scripts.
		/// </summary>
		/// <returns>the list of scripts attached to cameras</returns>
		/// 
		public static List<ScreenFade> AttachToAllCameras()
		{
			// get all cameras
			Camera[] cameras = new Camera[Camera.allCamerasCount];
			Camera.GetAllCameras(cameras);

			// find or add fade behaviour
			List<ScreenFade> faders = new List<ScreenFade>();
			foreach(Camera cam in cameras)
			{
				if (cam != null)
				{
					ScreenFade fade = cam.gameObject.GetComponent<ScreenFade>();
					if (fade == null)
					{
						fade = cam.gameObject.AddComponent<ScreenFade>();
					}
					faders.Add(fade);
				}
			}

			return faders;
		}


		private Material m_FadeMaterial;
	}
}
