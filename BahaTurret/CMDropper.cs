using System;
using UnityEngine;

namespace BahaTurret
{
	public class CMDropper : PartModule
	{
		AudioSource audioSource;
		AudioClip deploySound = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/flareSound");
		
		[KSPAction("Drop Countermeasure")]
		public void AGDropCM(KSPActionParam param)
		{
			DropCM();
		}
		
		[KSPEvent(guiActive = true, guiName = "Drop Countermeasure", active = true)]
		public void DropCM()
		{
			PartResource cmResource = GetCMResource();
			if(cmResource.amount >= 1)
			{
				cmResource.amount--;
				Debug.Log("Dropping counterMeasure");
				audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
				audioSource.PlayOneShot(deploySound);
				GameObject cm = GameDatabase.Instance.GetModel("BDArmory/Models/CMFlare/model");
				cm = (GameObject) Instantiate(cm, transform.position, transform.rotation);
				CMFlare cmf = cm.AddComponent<CMFlare>();
				cmf.startVelocity = rigidbody.velocity + (30*transform.up) + (UnityEngine.Random.Range(-3f,3f) * transform.forward) + (UnityEngine.Random.Range(-3f,3f) * transform.right);
				cmf.sourceVessel = vessel;
				cm.SetActive(true);
			}
		}
		
		public override void OnStart (PartModule.StartState state)
		{
			part.force_activate();
			
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.volume = Mathf.Sqrt(GameSettings.SHIP_VOLUME);
			audioSource.minDistance = 1;
			audioSource.maxDistance = 1000;
		}

		public override void OnUpdate ()
		{
			if(audioSource)
			{
				if(vessel.isActiveVessel)
				{
					audioSource.dopplerLevel = 0;
				}
				else
				{
					audioSource.dopplerLevel = 1;
				}
			}
		}

		
		PartResource GetCMResource()
		{
			foreach(var res in part.Resources.list)
			{
				if(res.resourceName == "Countermeasure") return res;	
			}
			
			return null;
		}
		
	}
}

