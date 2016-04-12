using UnityEngine;
using System.Collections;

// https://forums.oculus.com/community/discussion/3413/calculating-the-distortion-shader-parameters

[ExecuteInEditMode]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]

public class LensDistortion : MonoBehaviour
{
	public Vector4 DistortionCoefficients = new Vector4(1, 0, 0, 0);
	public Vector4 ChromaticAberration    = new Vector4(0, 0, 0, 0);
	public Vector2 Center                 = new Vector2(0.5f, 0.5f);
	public float   ScaleIn                = 1;
	public float   ScaleOut               = 1;
	public Shader  DistortionShader;

	private Material DistortionMaterial;


	public void Start()
	{
		DistortionMaterial = new Material(DistortionShader);
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		DistortionMaterial.SetVector("_Distortion",          DistortionCoefficients);
		DistortionMaterial.SetVector("_ChromaticAberration", ChromaticAberration);
		DistortionMaterial.SetVector("_Center",              Center);
		DistortionMaterial.SetFloat( "_ScaleIn",  ScaleIn);
		DistortionMaterial.SetFloat( "_ScaleOut", ScaleOut);
		Graphics.Blit(source, destination, DistortionMaterial);
	}


    /// <summary>
    /// Copy parameters from the VR Display configuration structure to the shader.
    /// </summary>
    /// <param name="config">the VR Display configuration structure to copy from</param>
    /// 
    public void ApplyConfig(VR.HMD_Config config)
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


    }
}
