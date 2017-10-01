using System;
using BDArmory.Core.Extension;
using UnityEngine;

namespace BDArmory.Core.Module
{
    public class DamageTracker : PartModule
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Damage"),UI_ProgressBar(affectSymCounterparts = UI_Scene.None,controlEnabled = false,scene = UI_Scene.All)]
        public float Damage = 0f;

        //TODO: Add setting
        private readonly float maxDamageFactor = 800f;

        private MaterialColorUpdater damageRenderer;

    
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            Debug.Log("[BDArmory]:DamageTracker started");
            damageRenderer = new MaterialColorUpdater(this.part.transform, PhysicsGlobals.TemperaturePropertyID);

            if (part != null)
            {
                UI_ProgressBar damageField = (UI_ProgressBar) Fields["Damage"].uiControlEditor;
                damageField.maxValue = (float) maxDamageFactor * part.mass * Mathf.Clamp(part.crashTolerance, 1, 100);
                Debug.Log("[BDArmory]:DamageTracker started. MaxValue =" + damageField.maxValue);
                damageField.minValue = 0f;
                this.part.RefreshAssociatedWindows();
            }
            else
            {
                Debug.Log("[BDArmory]:DamageTracker::OnStart part  is null");
            }
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
            
            damageRenderer?.Update(PhysicsGlobals.GetBlackBodyRadiation(this.Damage, this.part), false);
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
            Debug.Log("[BDArmory]:DamageTracker SetDamage.damage"+ Damage + " MaxValue =" + this.GetMaxPartDamage());
        }

        public void AddDamage(float damage)
        {
            this.Damage += damage;
            if (this.Damage > this.GetMaxPartDamage())
            {
                this.DestroyPart();
            }
            Debug.Log("[BDArmory]:DamageTracker AddDamage" + Damage + " MaxValue =" + this.GetMaxPartDamage());
        }
    }
}
