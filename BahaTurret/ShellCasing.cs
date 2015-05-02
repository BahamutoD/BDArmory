using System;
using UnityEngine;

namespace BahaTurret
{
	public class ShellCasing : MonoBehaviour
	{
		public float startTime;
		public Vector3 initialV;
		
		
		void Start()
		{
			startTime = Time.time;	
			gameObject.AddComponent<Rigidbody>();
			rigidbody.mass = 0.001f;
			rigidbody.velocity = initialV;
			rigidbody.AddRelativeForce(new Vector3 (UnityEngine.Random.Range(-.1f,.1f), UnityEngine.Random.Range(-.1f,.1f), UnityEngine.Random.Range(6f,8f)) , ForceMode.VelocityChange);
			rigidbody.AddRelativeTorque(new Vector3(UnityEngine.Random.Range(0f,10f),UnityEngine.Random.Range(0f,10f),UnityEngine.Random.Range(0f,10f)), ForceMode.VelocityChange);
			if(!FlightGlobals.RefFrameIsRotating)
			{
				rigidbody.useGravity = false;
			}

		}
		
		void FixedUpdate()
		{
			//atmospheric drag
			rigidbody.AddForce((-0.0005f) * rigidbody.velocity * (float) FlightGlobals.getStaticPressure(transform.position, FlightGlobals.currentMainBody));
			
			if (Time.time - startTime > 2)
			{
				GameObject.Destroy(gameObject);	
			}
		}
	}
}

