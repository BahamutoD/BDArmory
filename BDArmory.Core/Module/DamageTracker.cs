using System;
using BDArmory.Core.Extension;
using UnityEngine;

namespace BDArmory.Core.Module
{
    public class DamageTracker : PartModule
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Damage"),UI_ProgressBar(affectSymCounterparts = UI_Scene.None,controlEnabled = false,scene = UI_Scene.All,maxValue = 100000,minValue = 0,requireFullControl = false)]
        public float Damage = 0f;

        //TODO: Add setting
        private readonly float maxDamageFactor = 800f;

        private MaterialColorUpdater damageRenderer;
        private Gradient g = new Gradient();
        
    
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            damageRenderer = new MaterialColorUpdater(this.part.transform, PhysicsGlobals.TemperaturePropertyID);

            if (part != null)
            {
                UI_ProgressBar damageFieldFlight = (UI_ProgressBar)Fields["Damage"].uiControlFlight;
                damageFieldFlight.maxValue = CalculateMaxDamage();
                damageFieldFlight.minValue = 0f;

                UI_ProgressBar damageFieldEditor = (UI_ProgressBar)Fields["Damage"].uiControlEditor;

                damageFieldEditor.maxValue = CalculateMaxDamage();
                damageFieldEditor.minValue = 0f;
                this.part.RefreshAssociatedWindows();
            }
            else
            {
                Debug.Log("[BDArmory]:DamageTracker::OnStart part  is null");
            }
        }

        private float CalculateMaxDamage()
        {
            return maxDamageFactor * part.mass * Mathf.Clamp(part.crashTolerance, 1, 100);
        }

        public void DestroyPart()
        {
            this.part.temperature = this.part.maxTemp * 2;
        }


        public float GetMaxPartDamage()
        {
            UI_ProgressBar damageField = (UI_ProgressBar) Fields["Damage"].uiControlEditor;

            return damageField.maxValue;
        }


        public override void OnUpdate()
        {
            //TODO: Add effects
            if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready || this.Damage == 0f)
            {
                    return;
            }
            
            damageRenderer?.Update(GetDamageColor());
        }

        public  Color GetDamageColor()
        {
            Color color = PhysicsGlobals.BlackBodyRadiation.Evaluate(Mathf.Clamp01(part.Damage() / part.MaxDamage()));
            color.a *= PhysicsGlobals.BlackBodyRadiationAlphaMult * part.blackBodyRadiationAlphaMult; ;
            return color;
        }


        void OnDestroy()
        {

           

        }

        public void SetDamage(float damage)
        {
            this.Damage = damage;
            if (this.Damage > this.GetMaxPartDamage())
            {
                this.DestroyPart();
            }
        }

        public void AddDamage(float damage)
        {
            this.Damage += damage;
            if (this.Damage > this.GetMaxPartDamage())
            {
                this.DestroyPart();
            }
        }
    }
}
