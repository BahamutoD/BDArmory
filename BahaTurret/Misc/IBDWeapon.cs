using System;
using UnityEngine;
namespace BahaTurret
{
	public interface IBDWeapon 
	{
		WeaponClasses GetWeaponClass();
		string GetShortName();
		string GetSubLabel();
		Part GetPart();

        // extensions for feature_engagementenvelope
        float GetEngagementRangeMin();
        float GetEngagementRangeMax();
        bool GetEngageAirTargets();
        bool GetEngageMissileTargets();
        bool GetEngageGroundTargets();
    }


    // extensions for feature_engagementenvelope
    public abstract class ABDWeapon : PartModule
    {
        // Weapon usage settings
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "EngageRangeMin"),
            UI_FloatRange(minValue = 0f, maxValue = 5000f, stepIncrement = 100f, scene = UI_Scene.Editor)]
        public float engageRangeMin = 0;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "EngageRangeMax"),
            UI_FloatRange(minValue = 0f, maxValue = 5000f, stepIncrement = 100f, scene = UI_Scene.Editor)]
        public float engageRangeMax = 0;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "EngageAir"),
            UI_Toggle(disabledText = "false", enabledText = "true")]
        public bool engageAir = true;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "EngageMissile"),
            UI_Toggle(disabledText = "false", enabledText = "true")]
        public bool engageMissile = true;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "EngageGround"),
            UI_Toggle(disabledText = "false", enabledText = "true")]
        public bool engageGround = true;



        public void OnRangeUpdated(BaseField field, object obj)
        {
            // ensure max >= min
            if (engageRangeMax < engageRangeMin)
                engageRangeMax = engageRangeMin;

        }

        protected void InitializeEngagementRange(float min, float max)
        {

            var rangeMin = (UI_FloatRange)this.Fields["engageRangeMin"].uiControlEditor;
            rangeMin.minValue = min;
            rangeMin.maxValue = max;
            rangeMin.onFieldChanged = OnRangeUpdated;

            var rangeMax = (UI_FloatRange)this.Fields["engageRangeMax"].uiControlEditor;
            rangeMax.minValue = min;
            rangeMax.maxValue = max;
            rangeMax.onFieldChanged = OnRangeUpdated;

            if ((engageRangeMin == 0) && (engageRangeMax == 0))
            {
                // no sensible settings yet, set to default
                engageRangeMin = min;
                engageRangeMax = max;
            }
        }


        //implementations from Interface
        public float GetEngagementRangeMin()
        {
            return engageRangeMin;
        }

        public float GetEngagementRangeMax()
        {
            return engageRangeMax;
        }

        public bool GetEngageAirTargets()
        {
            return engageAir;
        }

        public bool GetEngageMissileTargets()
        {
            return engageMissile;
        }

        public bool GetEngageGroundTargets()
        {
            return engageGround;
        }

    }


    public enum WeaponClasses{Missile, Bomb, Gun, Rocket, DefenseLaser}
}

