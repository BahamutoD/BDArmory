using UnityEngine;

namespace BDArmory.Parts
{
	public class TGPCamRotator : MonoBehaviour 
	{

		void OnPreRender()
		{
			if(TargetingCamera.Instance)
			{
				TargetingCamera.Instance.UpdateCamRotation(transform);
			}
		}
	}
}

