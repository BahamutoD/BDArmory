using BDArmory.Core.Extension;
using UnityEngine;

namespace BDArmory.Core.Module
{
    public class HitpointTracker : PartModule
    {
        #region KSP Fields

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Hitpoints"),
        UI_ProgressBar(affectSymCounterparts = UI_Scene.None,controlEnabled = false,scene = UI_Scene.All,maxValue = 100000,minValue = 0,requireFullControl = false)]
        public float Hitpoints;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Armor thickness"),
        UI_FloatRange(minValue = 1f, maxValue = 500f, stepIncrement = 5f, scene = UI_Scene.All)]
        public float Armor = 10f;

        [KSPField(isPersistant = true)]
        public bool ArmorSet;

        [KSPField(isPersistant = true)]
        public bool HitpointSet = false;

        #endregion

        //TODO: Add setting
        private readonly float maxHitpointFactor = 100f;

        private Part _prefabPart;
        private bool _setupRun;
        private bool _firstSetup = true;

        protected virtual void Setup()
        {
            if (_setupRun)
            {
                return;
            }
            _prefabPart = part.partInfo.partPrefab;
            _setupRun = true;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (part.partInfo == null)
            {
                // Loading of the prefab from the part config
                _prefabPart = part;
                SetupPrefab();

            }
            else
            {
                // Loading of the part from a saved craft                
                if (HighLogic.LoadedSceneIsEditor)
                    Setup();
                else
                    enabled = false;
            }
        }

        private float previousHitpoints;

        protected virtual void SetupPrefab()
        {
            //var PartNode = GameDatabase.Instance.GetConfigs("PART").FirstOrDefault(c => c.name.Replace('_', '.') == part.name).config;
            //var ModuleNode = PartNode.GetNodes("MODULE").FirstOrDefault(n => n.GetValue("name") == moduleName);

            //ScaleType = new ScaleType(ModuleNode);
            //SetupFromConfig(ScaleType);
            //tweakScale = currentScale = defaultScale;

            if (part != null)
            {
                var maxHitPoints = CalculateMaxDamage();

                if (previousHitpoints == maxHitPoints) return;

                //Add Hitpoints
                UI_ProgressBar damageFieldFlight = (UI_ProgressBar)Fields["Hitpoints"].uiControlFlight;
                damageFieldFlight.maxValue = maxHitPoints;
                damageFieldFlight.minValue = 0f;

                UI_ProgressBar damageFieldEditor = (UI_ProgressBar)Fields["Hitpoints"].uiControlEditor;
                damageFieldEditor.maxValue = maxHitPoints;
                damageFieldEditor.minValue = 0f;

                Hitpoints = maxHitPoints;

                //Add Armor
                UI_FloatRange armorFieldFlight = (UI_FloatRange)Fields["Armor"].uiControlFlight;
                armorFieldFlight.maxValue = 500f;
                armorFieldFlight.minValue = 10;

                UI_FloatRange armorFieldEditor = (UI_FloatRange)Fields["Armor"].uiControlEditor;
                armorFieldEditor.maxValue = 500f;
                armorFieldEditor.minValue = 10f;

                part.RefreshAssociatedWindows();

                if (!ArmorSet) SetThickness();


                previousHitpoints = Hitpoints;
            }
            else
            {
                Debug.Log("[BDArmory]:HitpointTracker::OnStart part  is null");
            }
        }

        public override void OnStart(StartState state)
        {
            isEnabled = true;

            if (part != null && _firstSetup) SetupPrefab();

            if (HighLogic.LoadedSceneIsFlight)
            {
                UI_FloatRange armorField = (UI_FloatRange)Fields["Armor"].uiControlFlight;
                //Once started the max value of the field should be the initial one
                armorField.maxValue = Armor;
                this.part.RefreshAssociatedWindows();
            }

            //damageRenderer = new MaterialColorUpdater(this.part.transform, PhysicsGlobals.TemperaturePropertyID);          
        }

        private void ShipModified(ShipConstruct data)
        {
            SetupPrefab();
        }

        void OnGUI()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                SetupPrefab();
            }
        }


        public override void OnUpdate()
        {
            //TODO: Add effects
            if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready || Hitpoints == 0f)
            {
                return;
            }

            if (part != null && _firstSetup)
            {
                _firstSetup = false;
                SetupPrefab();
             }
           // damageRenderer?.Update(GetDamageColor());         
        }        
        #region Hitpoints Functions

        private float CalculateMaxDamage()
        {
            float maxDamage;

            if (!part.IsMissile())
            {

                //1. Density of the dry mass of the part.
                var density = part.GetDensity();
                //2. Lets simulate a new volume based on the surface of the part and crashtolerance
                var simulatedVolumen = part.GetArea() * Mathf.Clamp(part.crashTolerance / 100f, 0.01f, 0.30f);
                //3. final calculations 
                maxDamage = simulatedVolumen * density * 5f;

                Debug.Log("[BDArmory]:CalculateMaxDamage MaxDamage ="+ maxDamage +" | Density ="+density+"| simulatedVolumen ="+simulatedVolumen);
            }
            else
            {
                maxDamage = 5;
                Armor = 5;
            }
            return maxDamage;
        }

        public void DestroyPart()
        {
            //part.explode();
            part.temperature = part.maxTemp * 2;
        }

        public float GetMaxArmor()
        {
            UI_FloatRange armorField = (UI_FloatRange)Fields["Armor"].uiControlEditor;

            return armorField.maxValue;
        }

        public float GetMaxHitpoints()
        {
            UI_ProgressBar hitpointField = (UI_ProgressBar) Fields["Hitpoints"].uiControlEditor;

            return hitpointField.maxValue;
        }        

        public void SetDamage(float partdamage)
        {
            Hitpoints -= partdamage;

            if(Hitpoints <= 0)
            {
                DestroyPart();
            }
        }

        public void AddDamage(float partdamage)
        {
            if (part.name == "Weapon Manager" || part.name == "BDModulePilotAI") return;

            partdamage = Mathf.Max(partdamage, 0.01f) * -1;

            Hitpoints += partdamage;

            if (Hitpoints <= 0)
            {
                DestroyPart();
            }
        }

        public void ReduceArmor(float massToReduce)
        {
            Armor -= massToReduce;
            if (Armor < 0)
            {
                Armor = 0;
            }
        }

        public void SetThickness(float thickness = 0)
        {
            ArmorSet = true;

            if (part.FindModuleImplementing<BDArmor>())
            {                
                float armor = part.FindModuleImplementing<BDArmor>().ArmorThickness;

                if (armor != 0)
                {
                    Armor = armor;
                }                
            }
        }
      
        #endregion

    }
}
