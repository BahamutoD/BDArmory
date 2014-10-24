using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class Misc
	{
		
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
		
		public static AnimationState[] SetUpAnimation(string animationName, Part part)  //Thanks Majiir!
        {
            var states = new List<AnimationState>();
            foreach (var animation in part.FindModelAnimators(animationName))
            {
                var animationState = animation[animationName];
                animationState.speed = 0;
                animationState.enabled = true;
                animationState.wrapMode = WrapMode.ClampForever;
                animation.Blend(animationName);
                states.Add(animationState);
            }
            return states.ToArray();
        }
		
		public static bool CheckMouseIsOnGui()
		{
			Vector3 inverseMousePos = new Vector3(Input.mousePosition.x, Screen.height-Input.mousePosition.y, 0);
			Rect topGui = new Rect(0,0, Screen.width, 65);
			
			return 
			(
				BDArmorySettings.GAME_UI_ENABLED && 
				BDArmorySettings.FIRE_KEY.Contains("mouse") &&
				(
					(BDArmorySettings.toolbarGuiEnabled && BDArmorySettings.Instance.toolbarWindowRect.Contains(inverseMousePos)) || 
					topGui.Contains(inverseMousePos)
				)
			);	
		}
		
	
	}
}

