using System;
using UnityEngine;
namespace BahaTurret
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

