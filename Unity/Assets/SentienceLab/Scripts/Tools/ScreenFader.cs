#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab.PostProcessing
{
	/// <summary>
	/// Class for fading the screen using postprocessing.
	/// </summary>

	[DisallowMultipleComponent]
	[AddComponentMenu("Input/Input Manager")]
	public class ScreenFader : MonoBehaviour
	{
		[Tooltip("The colour to fade to")]
		public Color FadeColour = Color.black;

		[Tooltip("The fade factor (0: no fade, 1: completely faded)")]
		[Range(0, 1)]
		public float FadeFactor = 0;


		public void Awake()
		{
			Shader shader = Shader.Find("Hidden/Sentience Lab/Post Processing/Fade");
			m_FadeMaterial = new Material(shader);
		}


		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			m_FadeMaterial.SetColor("_Colour", FadeColour);
			m_FadeMaterial.SetFloat("_Fade", FadeFactor);
			Graphics.Blit(source, destination, m_FadeMaterial);
		}


		public static List<ScreenFader> AttachToAllCameras()
		{
			Camera[] cameras = Camera.allCameras;
			List<ScreenFader> faders = new List<ScreenFader>(cameras.Length);
			foreach(Camera cam in cameras)
			{
				ScreenFader fade = cam.gameObject.GetComponent<ScreenFader>();
				if (fade == null)
				{
					fade = cam.gameObject.AddComponent<ScreenFader>();
				}
				faders.Add(fade);
			}
			return faders;
		}


		private Material m_FadeMaterial;
	}
}
