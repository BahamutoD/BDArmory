using BDArmory.Core.Extension;
using UnityEngine;

namespace BDArmory.Core.Module
{
    public class HitpointTracker : PartModule
    {
        #region KSP Fields

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Hitpoints"),
        UI_ProgressBar(affectSymCounterparts = UI_Scene.None, controlEnabled = false, scene = UI_Scene.All, maxValue = 100000, minValue = 0, requireFullControl = false)]
        public float Hitpoints;

        [KSPField]
        bool hasPrefabRun = false;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Armor Thickness"),
        UI_FloatRange(minValue = 1f, maxValue = 500f, stepIncrement = 5f, scene = UI_Scene.All)]
        public float Armor = 10f;

        [KSPField(isPersistant = true)]
        public float maxHitPoints = 0f;

        [KSPField(isPersistant = true)]
        public float ArmorThickness = 0f;

        [KSPField(isPersistant = true)]
        public bool ArmorSet;

        [KSPField(isPersistant = true)]
        public string ExplodeMode = "Never";

        [KSPField(isPersistant = true)]
        public bool FireFX = true;

        [KSPField(isPersistant = true)]
        public float FireFXLifeTimeInSeconds = 5f;

        #endregion

        private readonly float hitpointMultiplier = BDArmorySettings.HITPOINT_MULTIPLIER;

        private float previousHitpoints;
        private Part _prefabPart;
        private bool _setupRun = false;
        private bool _firstSetup = true;

        private const float mk1CockpitArea = 7.292964f;
        private const int HpRounding = 250;


        public void Setup()
        {
            if (_setupRun)
            {
                return;
            }
            //_prefabPart = part.partInfo.partPrefab;
            _setupRun = true;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight) return;

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

        public void SetupPrefab()
        {
            _prefabPart = part.partInfo.partPrefab;

            if (part != null)
            {
                var maxHitPoints_ = CalculateTotalHitpoints();

                if (previousHitpoints == maxHitPoints_) return;

                //Add Hitpoints
                UI_ProgressBar damageFieldFlight = (UI_ProgressBar)Fields["Hitpoints"].uiControlFlight;
                damageFieldFlight.maxValue = maxHitPoints_;
                damageFieldFlight.minValue = 0f;

                UI_ProgressBar damageFieldEditor = (UI_ProgressBar)Fields["Hitpoints"].uiControlEditor;
                damageFieldEditor.maxValue = maxHitPoints_;
                damageFieldEditor.minValue = 0f;

                Hitpoints = maxHitPoints_;

                //Add Armor
                UI_FloatRange armorFieldFlight = (UI_FloatRange)Fields["Armor"].uiControlFlight;
                armorFieldFlight.maxValue = 500f;
                armorFieldFlight.minValue = 10;

                UI_FloatRange armorFieldEditor = (UI_FloatRange)Fields["Armor"].uiControlEditor;
                armorFieldEditor.maxValue = 500f;
                armorFieldEditor.minValue = 10f;

                part.RefreshAssociatedWindows();

                if (!ArmorSet) overrideArmorSetFromConfig();

                previousHitpoints = maxHitPoints_;
                hasPrefabRun = true;
            }
            else
            {

                Debug.Log("[BDArmory]: HitpointTracker::OnStart part is null");
            }
        }

        public override void OnStart(StartState state)
        {
            isEnabled = true;

            if (part != null) SetupPrefab();

            if (HighLogic.LoadedSceneIsFlight)
            {
                UI_FloatRange armorField = (UI_FloatRange)Fields["Armor"].uiControlFlight;
                //Once started the max value of the field should be the initial one
                armorField.maxValue = Armor;
                part.RefreshAssociatedWindows();
            }
            GameEvents.onEditorShipModified.Add(ShipModified);
        }

        private void OnDestroy()
        {
            GameEvents.onEditorShipModified.Remove(ShipModified);
        }

        public void ShipModified(ShipConstruct data)
        {
            SetupPrefab();
        }

        void OnGUI()
        {
            //if (HighLogic.LoadedSceneIsEditor)
            //{
            //    SetupPrefab();
            //}
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready || Hitpoints == 0f)
            {
                return;
            }

            if (part != null && _firstSetup)
            {
                _firstSetup = false;
                SetupPrefab();
            }

            if (HighLogic.LoadedSceneIsEditor)
            {
                //if (_ScalingFactor.index >= 0f)
                //{
                //    var changed = _ScalingFactor.index != (isFreeScale ? tweakScale : ScaleFactors[tweakName]);
                //    if (changed) // user has changed the scale tweakable
                //    {
                //        //OnTweakScaleChanged();
                //    }
                //}
            }
        }

        #region Hitpoints Functions



        public float CalculateTotalHitpoints()
        {
            float hitpoints;

            if (!part.IsMissile())
            {
                //1. Density of the dry mass of the part.
                var density = part.GetDensity();

                Debug.Log("[BDArmory]: Hitpoint Calc | Mesh : " + part.GetComponentInChildren<MeshFilter>().mesh.bounds.size);
                var tweakScaleModule = part.Modules["TweakScale"];

                if (tweakScaleModule != null)
                {
                    Debug.Log("[BDArmory]: Hitpoint Calc | scale : " + tweakScaleModule.Fields["currentScale"].GetValue<float>(tweakScaleModule) / tweakScaleModule.Fields["defaultScale"].GetValue<float>(tweakScaleModule));
                }

                Debug.Log("[BDArmory]: Hitpoint Calc | Density : " + Mathf.Round(density));
              
                //Structural mass is the area in m2 * 2 cm of thickness * material density

                var structuralMass = part.GetArea() * 0.02f * density;

                Debug.Log("[BDArmory]: Hitpoint Calc | structuralMass : " + Mathf.Round(structuralMass));


                //3. final calculations 
                hitpoints = structuralMass * 10 * hitpointMultiplier;
                hitpoints = Mathf.Round(hitpoints / HpRounding) * HpRounding;

                if (hitpoints <= 0) hitpoints = HpRounding;
            }
            else
            {
                hitpoints = 5;
                Armor = 2;
            }

            //override based on part configuration for custom parts
            if (maxHitPoints != 0)
            {
                hitpoints = maxHitPoints;
            }

            if (hitpoints <= 0) hitpoints = HpRounding;
            return hitpoints;
        }

        public void DestroyPart()
        {
            if (part.mass <= 2f) part.explosionPotential *= 0.85f;

            PartExploderSystem.AddPartToExplode(part);
        }

        public float GetMaxArmor()
        {
            UI_FloatRange armorField = (UI_FloatRange)Fields["Armor"].uiControlEditor;
            return armorField.maxValue;
        }

        public float GetMaxHitpoints()
        {
            UI_ProgressBar hitpointField = (UI_ProgressBar)Fields["Hitpoints"].uiControlEditor;
            return hitpointField.maxValue;
        }

        public bool GetFireFX()
        {
            return FireFX;
        }

        public void SetDamage(float partdamage)
        {
            Hitpoints -= partdamage;

            if (Hitpoints <= 0)
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

        public void AddDamageToKerbal(KerbalEVA kerbal, float damage)
        {
            damage = Mathf.Max(damage, 0.01f) * -1;
            Hitpoints += damage;

            if (Hitpoints <= 0)
            {
                // oh the humanity!
                PartExploderSystem.AddPartToExplode(kerbal.part);
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

        public void overrideArmorSetFromConfig(float thickness = 0)
        {
            ArmorSet = true;
            if (ArmorThickness != 0)
            {
                Armor = ArmorThickness;
            }
        }

        #endregion

    }
}