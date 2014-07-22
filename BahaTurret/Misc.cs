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
		
		public static Color ParseColor255(string color)
		{
			Color outputColor = new Color(0,0,0,1);
			
			var strings = color.Split(","[0]);
			for(int i = 0; i < 4; i++)
			{
				outputColor[i] = System.Single.Parse(strings[i])/255;	
			}
			
			return outputColor;
		}
		
		
	}
}

