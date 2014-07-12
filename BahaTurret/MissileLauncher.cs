using System;
using UnityEngine;

namespace BahaTurret
{
	public class MissileLauncher : PartModule
	{
		
		GameObject decoupleNode;
		GameObject missile;
		bool hasFired = false;
		
		
		public override void OnStart (PartModule.StartState state)
		{
			foreach(Transform tf in part.FindModelTransforms("decouple"))
			{
				decoupleNode = tf.gameObject;	
			}
			foreach(Transform tf in part.FindModelTransforms("missileTransform"))
			{
				missile = tf.gameObject;	
			}
			
			
			
			
		}
		
		
		
		
		[KSPAction("Fire Missile")]
		public void AGFire(KSPActionParam param)
		{
			FireMissile();	
		}
		
		[KSPEvent(guiActive = true, guiName = "Fire Missile", active = true)]
		public void GuiFire()
		{
			FireMissile();	
		}
		
		
		public void FireMissile()
		{
			if(!hasFired)
			{
				missile.AddComponent<Rigidbody>();
				missile.rigidbody.mass = 0.152f;
				decoupleNode.transform.DetachChildren();
				this.rigidbody.mass = 0.01f;
				missile.rigidbody.velocity = rigidbody.velocity;
				missile.rigidbody.AddRelativeForce(new Vector3(0,-2,0), ForceMode.VelocityChange);
				HomingMissile hm = missile.AddComponent<HomingMissile>();
				
				try
				{
					hm.target = vessel.targetObject.GetVessel();
				}
				catch(NullReferenceException){}
				
				
				hasFired = true;
				
			}
		}
		
		
		
		
		public override void OnFixedUpdate()
		{
			
		}
	}
}

