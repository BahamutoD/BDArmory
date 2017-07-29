using UnityEngine;

namespace BDArmory.FX
{
	public class CameraBulletRenderer : MonoBehaviour
	{
		public float resizeFactor = 1;
		Camera cam;

		void Awake()
		{
			cam = GetComponent<Camera>();
		}

		void OnPreRender()
		{
			if(ModuleWeapon.bulletPool)
			{
				for(int i = 0; i < ModuleWeapon.bulletPool.size; i++)
				{
					if(ModuleWeapon.bulletPool.GetPooledObject(i).activeInHierarchy)
					{
						PooledBullet pBullet = ModuleWeapon.bulletPool.GetPooledObject(i).GetComponent<PooledBullet>();
						pBullet.UpdateWidth(cam, resizeFactor);
					}
		
				}
			}
		}
	}
}

