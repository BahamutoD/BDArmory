using System.Collections.Generic;
using UnityEngine;

namespace BDArmory.Control
{
    public class BDAirspeedControl : MonoBehaviour //: PartModule
    {
        //[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "TargetSpeed"),
        //	UI_FloatRange(minValue = 1f, maxValue = 420f, stepIncrement = 1f, scene = UI_Scene.All)]
        public float targetSpeed = 0;

        public bool useBrakes = true;
        public bool allowAfterburner = true;

        //[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "ThrottleFactor"),
        //	UI_FloatRange(minValue = 1f, maxValue = 20f, stepIncrement = .5f, scene = UI_Scene.All)]
        public float throttleFactor = 2f;

        public Vessel vessel;


        bool controlEnabled;

        //[KSPField(guiActive = true, guiName = "Thrust")]
        public float debugThrust;

        List<MultiModeEngine> multiModeEngines;

        //[KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "ToggleAC")]
        public void Toggle()
        {
            if (controlEnabled)
            {
                Deactivate();
            }
            else
            {
                Activate();
            }
        }

        public void Activate()
        {
            controlEnabled = true;
            vessel.OnFlyByWire -= AirspeedControl;
            vessel.OnFlyByWire += AirspeedControl;
            multiModeEngines = new List<MultiModeEngine>();
        }

        public void Deactivate()
        {
            controlEnabled = false;
            vessel.OnFlyByWire -= AirspeedControl;
        }

        void AirspeedControl(FlightCtrlState s)
        {
            if (targetSpeed == 0)
            {
                vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                s.mainThrottle = 0;
                return;
            }


            float currentSpeed = (float) vessel.srfSpeed;
            float speedError = targetSpeed - currentSpeed;

            float setAccel = speedError*throttleFactor;

            SetAcceleration(setAccel, s);
        }


        void SetAcceleration(float accel, FlightCtrlState s)
        {
            float gravAccel = GravAccel();
            float requestEngineAccel = accel - gravAccel;

            possibleAccel = gravAccel;

            float dragAccel = 0;
            float engineAccel = MaxEngineAccel(requestEngineAccel, out dragAccel);

            if (engineAccel == 0)
            {
                s.mainThrottle = 0;
                return;
            }

            requestEngineAccel = Mathf.Clamp(requestEngineAccel, -engineAccel, engineAccel);

            float requestThrottle = (requestEngineAccel - dragAccel)/engineAccel;

            s.mainThrottle = Mathf.Clamp01(requestThrottle);

            //use brakes if overspeeding too much
            if (requestThrottle < -0.5f)
            {
                vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
            }
            else
            {
                vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
            }
        }

        float MaxEngineAccel(float requestAccel, out float dragAccel)
        {
            float maxThrust = 0;
            float finalThrust = 0;
            multiModeEngines.Clear();

            List<ModuleEngines>.Enumerator engines = vessel.FindPartModulesImplementing<ModuleEngines>().GetEnumerator();
            while (engines.MoveNext())
            {
                if (engines.Current == null) continue;
                if (!engines.Current.EngineIgnited) continue;

                MultiModeEngine mme = engines.Current.part.FindModuleImplementing<MultiModeEngine>();
                if (IsAfterBurnerEngine(mme))
                {
                    multiModeEngines.Add(mme);
                    mme.autoSwitch = false;
                }

                if (mme && mme.mode != engines.Current.engineID) continue;
                float engineThrust = engines.Current.maxThrust;
                if (engines.Current.atmChangeFlow)
                {
                  engineThrust *= engines.Current.flowMultiplier;
                }
                maxThrust += engineThrust*(engines.Current.thrustPercentage/100f);

                finalThrust += engines.Current.finalThrust;
            }
            engines.Dispose();

            debugThrust = maxThrust;

            float vesselMass = vessel.GetTotalMass();

            float accel = maxThrust/vesselMass;


            //estimate drag
            float estimatedCurrentAccel = finalThrust/vesselMass;
            float actualCurrentAccel = Vector3.Project(vessel.acceleration, vessel.ReferenceTransform.up).magnitude*
                                       Mathf.Sign(Vector3.Dot(vessel.acceleration, -vessel.ReferenceTransform.up));
            float accelError = (actualCurrentAccel - estimatedCurrentAccel)/2;
            dragAccel = accelError;

            possibleAccel += accel;

            //use multimode afterburner for extra accel if lacking
            List<MultiModeEngine>.Enumerator mmes = multiModeEngines.GetEnumerator();
            while (mmes.MoveNext())
            {
                if (mmes.Current == null) continue;
                if (allowAfterburner && (accel < requestAccel*0.2f || targetSpeed > 300))
                {
                    if (mmes.Current.runningPrimary)
                    {
                        mmes.Current.Events["ModeEvent"].Invoke();
                    }
                }
                else if (!allowAfterburner || accel > requestAccel*1.5f)
                {
                    if (!mmes.Current.runningPrimary)
                    {
                        mmes.Current.Events["ModeEvent"].Invoke();
                    }
                }
            }
            mmes.Dispose();
            return accel;
        }

        private static bool IsAfterBurnerEngine(MultiModeEngine engine)
        {
            if (engine == null)
            {
                return false;
            }
            if (!engine)
            {
                return false;
            }      
            return engine.primaryEngineID == "Dry" && engine.secondaryEngineID == "Wet";
        }

        float GravAccel()
        {
            Vector3 geeVector = FlightGlobals.getGeeForceAtPosition(vessel.CoM);
            float gravAccel = Vector3.Project(geeVector, vessel.ReferenceTransform.up).magnitude;
            gravAccel *= Mathf.Sign(Vector3.Dot(vessel.ReferenceTransform.up, geeVector));

            return gravAccel;
        }


        float possibleAccel;

        public float GetPossibleAccel()
        {
            return possibleAccel;
        }
    }
}