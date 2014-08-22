using System;
using UnityEngine;

namespace BahaTurret
{
	public class CMDropper : PartModule
	{
		//GameObject CMobject;
		
		[KSPAction("Drop Countermeasure")]
		public void AGDropCM(KSPActionParam param)
		{
			DropCM();
		}
		
		[KSPEvent(guiActive = true, guiName = "Drop Countermeasure", active = true)]
		public void DropCM()
		{
			Debug.Log("Dropping counterMeasure");
			GameObject cm = GameDatabase.Instance.GetModel("BDArmory/Models/CMFlare/model");
			cm = (GameObject) Instantiate(cm, transform.position, transform.rotation);
			CMFlare cmf = cm.AddComponent<CMFlare>();
			cmf.startVelocity = rigidbody.velocity + (20*transform.up);
			cm.SetActive(true);
			
		}
		
		public override void OnStart (PartModule.StartState state)
		{
			part.force_activate();
		}
		
	}
}

