using System;
using UnityEngine;

namespace BahaTurret
{
	public class Misc
	{
		public static void CreateSmoke(Vector3 position)
		{
			GameObject gameObject = new GameObject("smokeHit");
			gameObject.transform.position = position;
			
		}
	}
}

