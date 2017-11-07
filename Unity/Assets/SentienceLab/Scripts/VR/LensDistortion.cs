#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Script for applying parameters to the shader that simulates the in-built lens distortion of the Oculus Rift.
	/// 
	/// https://forums.oculus.com/community/discussion/3413/calculating-the-distortion-shader-parameters
	/// </summary>
	///

	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Camera))]

	[AddComponentMenu("VR/Lens Distortion")]

	public class LensDistortion : MonoBehaviour
	{
		public Vector4 DistortionCoefficients = new Vector4(1, 0, 0, 0);
		public Vector4 ChromaticAberration    = new Vector4(0, 0, 0, 0);
		public Vector2 Center                 = new Vector2(0.5f, 0.5f);
		public float   ScaleIn                = 1;
		public float   ScaleOut               = 1;


		public void Start()
		{
			Shader shader = Shader.Find("VR/LensDistortion");
			if (shader != null)
			{
				m_distortionMaterial = new Material(shader);
			}
			else
			{
				Debug.LogWarning("Shader 'VR/Lens Distortion' not found. Is it included in the list of preloaded shaders or in a 'Resources' folder?");
			}
		}


		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (m_distortionMaterial != null)
			{
				m_distortionMaterial.SetVector("_Distortion", DistortionCoefficients);
				m_distortionMaterial.SetVector("_ChromaticAberration", ChromaticAberration);
				m_distortionMaterial.SetVector("_Center", Center);
				m_distortionMaterial.SetFloat("_ScaleIn", ScaleIn);
				m_distortionMaterial.SetFloat("_ScaleOut", ScaleOut);
				Graphics.Blit(source, destination, m_distortionMaterial);
			}
			else
			{
				// something went wrong: show me at least the original image
				Graphics.Blit(source, destination);
			}
		}


		/// <summary>
		/// Copy parameters from the VR Display configuration structure to the shader.
		/// </summary>
		/// <param name="config">the VR Display configuration structure to copy from</param>
		/// 
		public void ApplyConfig(HMD_Config config)
		{
			for (int i = 0; i < 4; i++)
			{
				DistortionCoefficients[i] = config.LensDistortionParameters[i];
			}

			for (int i = 0; i < 2; i++)
			{
				// from two [2] arrays to one [4] vector
				ChromaticAberration[i + 0] = config.ChromaticAberrationParametersRed[i];
				ChromaticAberration[i + 2] = config.ChromaticAberrationParametersBlue[i];
			}

			ScaleIn  = config.ScaleIn;
			ScaleOut = config.ScaleOut;
		}


		private Material m_distortionMaterial;
	}

}