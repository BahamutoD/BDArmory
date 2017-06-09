using BDArmory.Shaders;
using BDArmory.UI;
using UnityEngine;

namespace BDArmory.Parts
{
	public class TGPCameraEffects : MonoBehaviour 
	{
		public static Material grayscaleMaterial;
		
		public Texture  textureRamp;
		public float    rampOffset;


		void Awake()
		{
			if(!grayscaleMaterial)
			{
				grayscaleMaterial = new Material(BDAShaderLoader.GrayscaleEffectShader);
				grayscaleMaterial.SetTexture("_RampTex", textureRamp);
                grayscaleMaterial.SetFloat("_RedPower", rampOffset);
                grayscaleMaterial.SetFloat("_RedDelta", rampOffset);
            }
		}
		


		void OnRenderImage (RenderTexture source, RenderTexture destination) 
		{
			if(BDArmorySettings.BW_TARGET_CAM || TargetingCamera.Instance.nvMode)
			{
				Graphics.Blit (source, destination, grayscaleMaterial); //apply grayscale
			}
		}
			

	}
}

