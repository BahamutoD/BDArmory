using System;
using System.Collections.Generic;
using KSP.UI.Screens;
using UniLinq;
using UnityEngine;

namespace BahaTurret
{
    public class BDModularGuidance : MissileBase
    {
        private DateTime _firedTime;
        private bool _firedTriggered;
        private int _nextStage = (int) KSPActionGroup.Custom01;

        private Vessel _parentVessel;
        private bool _readyForGuidance;

        private PartModule _targetDecoupler;

        private Vessel _targetVessel;

        private Transform _velocityTransform;

        private Transform _vesselTransform;

        public Vessel LegacyTargetVessel;

        public AudioSource SfAudioSource;

        public Vessel SourceVessel;

        public MissileFire TargetMf;

        private void RefreshGuidanceMode()
        {
            switch (GuidanceMode)
            {
                case 1:
                    GuidanceLabel = "AAM";
                    break;
                case 2:
                    GuidanceLabel = "AGM/STS";
                    break;
                case 3:
                    GuidanceLabel = "Cruise";
                    break;
            }

            if (Fields["CruiseAltitude"] != null)
            {
                Fields["CruiseAltitude"].guiActive = GuidanceMode == 3;
                Fields["CruiseAltitude"].guiActiveEditor = GuidanceMode == 3;
            }


            Misc.RefreshAssociatedWindows(part);
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            CheckGuidanceInit();

            CheckDelayedFired();

            CheckDetonationDistance();

            CheckNextStage();
        }

        private void CheckDetonationDistance()
        {
            if (_targetVessel == null) return;
            if (!HasFired) return;
            if (DetonationDistance == 0) return;

            if (!((_targetVessel.transform.position - vessel.transform.position).magnitude <=
                  DetonationDistance)) return;

            foreach (var highExplosive in vessel.FindPartModulesImplementing<BDExplosivePart>())
            {
                highExplosive.Detonate();
            }
        }

        private void CheckNextStage()
        {
            if (HasFired && ShouldExecuteNextStage())
            {
                ExecuteNextStage();
            }
        }

        private void CheckDelayedFired()
        {
            if (_firedTriggered && !HasFired)
            {
                if (DateTime.Now - _firedTime > TimeSpan.FromSeconds(DropTime))
                {
                    MissileIgnition();
                }
            }
        }

        private void MissileIgnition()
        {
            StartGuidance();
            ExecuteNextStage();
            HasFired = true;
        }

        private void CheckGuidanceInit()
        {
            if (_readyForGuidance && DateTime.Now - _firedTime > TimeSpan.FromSeconds(DropTime + 1))
            {
                _readyForGuidance = false;
                GuidanceActive = true;
                UpdateVesselTransform();

                var velocityObject = new GameObject("velObject");
                velocityObject.transform.position = transform.position;
                velocityObject.transform.parent = transform;
                _velocityTransform = velocityObject.transform;

                Events["StartGuidance"].guiActive = false;
                Misc.RefreshAssociatedWindows(part);
            }
        }

        private bool ShouldExecuteNextStage()
        {
            var ret = true;
            //If the next stage is greater than the number defined of stages the missile is done
            if (_nextStage > 128*(StagesNumber + 1))
            {
                return false;
            }

            foreach (
                var engine in
                    vessel.parts.Where(IsEngine).Select(x => x.FindModuleImplementing<ModuleEngines>()))
            {
                if (engine.EngineIgnited && !engine.getFlameoutState)
                {
                    ret = false;
                    break;
                }
            }
            return ret;
        }

        public bool IsEngine(Part p)
        {
            for (var i = 0; i < p.Modules.Count; i++)
            {
                var m = p.Modules[i];
                if (m is ModuleEngines)
                    return true;
            }
            return false;
        }

        public override void OnStart(StartState state)
        {
            Events["HideUI"].active = false;
            Events["ShowUI"].active = true;

            if (HighLogic.LoadedSceneIsEditor)
            {
                WeaponNameWindow.OnActionGroupEditorOpened.Add(OnActionGroupEditorOpened);
                WeaponNameWindow.OnActionGroupEditorClosed.Add(OnActionGroupEditorClosed);
            }
            if (string.IsNullOrEmpty(GetShortName()))
            {
                shortName = "Unnamed";
            }

            part.force_activate();

            UpdateVesselTransform();

            RefreshGuidanceMode();

            _targetDecoupler = FindFirstDecoupler(part.parent, null);

            weaponClass = WeaponClasses.Missile;
            WeaponName = GetShortName();
        }

        private void OnDestroy()
        {
            WeaponNameWindow.OnActionGroupEditorOpened.Remove(OnActionGroupEditorOpened);
            WeaponNameWindow.OnActionGroupEditorClosed.Remove(OnActionGroupEditorClosed);
        }

        private void UpdateVesselTransform()
        {
            if (part.vessel != null && part.vessel.vesselTransform != null)
            {
                _vesselTransform = part.vessel.vesselTransform;
                _parentVessel = vessel;

                part.SetReferenceTransform(_vesselTransform);
            }
        }

        public void GuidanceSteer(FlightCtrlState s)
        {
            if (GuidanceActive && _targetVessel != null && vessel != null && _vesselTransform != null &&
                _velocityTransform != null)
            {
                _velocityTransform.rotation = Quaternion.LookRotation(vessel.srf_velocity, -_vesselTransform.forward);
                var targetPosition = _targetVessel.CurrentCoM;
                var targetVelocity = _targetVessel.rb_velocity;
                var targetAcceleration = _targetVessel.acceleration;
                var localAngVel = vessel.angularVelocity;

                if (GuidanceMode == 1)
                {
                    float timeToImpact;
                    targetPosition = MissileGuidance.GetAirToAirTarget(targetPosition, targetVelocity,
                        targetAcceleration, vessel, out timeToImpact);
                    TimeToImpact = timeToImpact;
                }
                else if (GuidanceMode == 2)
                {
                    targetPosition = MissileGuidance.GetAirToGroundTarget(targetPosition, vessel, 1.85f);
                }
                else
                {
                    targetPosition = MissileGuidance.GetCruiseTarget(targetPosition, vessel, CruiseAltitude);
                }

                var targetDirection = _velocityTransform.InverseTransformPoint(targetPosition).normalized;
                targetDirection = Vector3.RotateTowards(Vector3.forward, targetDirection, 15*Mathf.Deg2Rad, 0);


                var steerYaw = SteerMult*targetDirection.x - SteerDamping*-localAngVel.z;
                var steerPitch = SteerMult*targetDirection.y - SteerDamping*-localAngVel.x;

                s.yaw = Mathf.Clamp(steerYaw, -MaxSteer, MaxSteer);
                s.pitch = Mathf.Clamp(steerPitch, -MaxSteer, MaxSteer);

                s.mainThrottle = 1;
            }
        }

        private void RefreshTargetingMode()
        {
            _targetingLabel = TargetingMode.ToString();
            Misc.RefreshAssociatedWindows(part);
        }

        private void UpdateMenus(bool visible)
        {
            Events["HideUI"].active = visible;
            Events["ShowUI"].active = !visible;
        }

        private void OnActionGroupEditorOpened()
        {
            Events["HideUI"].active = false;
            Events["ShowUI"].active = false;
        }

        private void OnActionGroupEditorClosed()
        {
            Events["HideUI"].active = false;
            Events["ShowUI"].active = true;
        }

        /// <summary>
        ///     Recursive method to find the top decoupler that should be used to jettison the missile.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        private PartModule FindFirstDecoupler(Part parent, PartModule last)
        {
            if (parent == null || !parent) return last;

            PartModule newModuleDecouple = parent.FindModuleImplementing<ModuleDecouple>();
            if (newModuleDecouple == null)
            {
                newModuleDecouple = parent.FindModuleImplementing<ModuleAnchoredDecoupler>();
            }
            if (newModuleDecouple != null && newModuleDecouple)
            {
                return FindFirstDecoupler(parent.parent, newModuleDecouple);
            }
            return FindFirstDecoupler(parent.parent, last);
        }

        /// <summary>
        ///     This method will execute the next ActionGroup. Due to StageManager is designed to work with an active vessel
        ///     And a missile is not an active vessel. I had to use a different way handle stages. And action groups works perfect!
        /// </summary>
        public void ExecuteNextStage()
        {
            part.vessel.OnFlyByWire -= GuidanceSteer;
            part.vessel.ActionGroups.ToggleGroup((KSPActionGroup) _nextStage);

            _nextStage *= 2;

            part.vessel.OnFlyByWire += GuidanceSteer;
        }

        #region KSP FIELDS

        [KSPField(isPersistant = false, guiActive = true, guiName = "Weapon Name ", guiActiveEditor = true)] public
            string WeaponName;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "CruiseAltitude"),
         UI_FloatRange(minValue = 50f, maxValue = 1500f, stepIncrement = 50f, scene = UI_Scene.All)] public float
            CruiseAltitude = 500;

        public bool GuidanceActive;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Guidance Type ", guiActiveEditor = true)] public
            string GuidanceLabel =
                "AGM/STS";


        [KSPField(isPersistant = true, guiActive = true, guiName = "Targeting Mode ", guiActiveEditor = true)] private
            string _targetingLabel = TargetingModes.Laser.ToString();

        [KSPField(isPersistant = true)] public int GuidanceMode = 2;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "SteerLimiter"),
         UI_FloatRange(minValue = .1f, maxValue = 1f, stepIncrement = .05f, scene = UI_Scene.All)] public float MaxSteer
             = 1;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "StagesNumber"),
         UI_FloatRange(minValue = 1f, maxValue = 5f, stepIncrement = 1f, scene = UI_Scene.All)] public float
            StagesNumber = 1;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Drop time"),
         UI_FloatRange(minValue = 0f, maxValue = 5f, stepIncrement = 0.1f, scene = UI_Scene.All)] public float
            DropTime;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Detonation distance"),
         UI_FloatRange(minValue = 0f, maxValue = 500f, stepIncrement = 1f, scene = UI_Scene.All)] public float
            DetonationDistance;


        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "SteerDamping"),
         UI_FloatRange(minValue = 0f, maxValue = 20f, stepIncrement = .05f, scene = UI_Scene.All)] public float
            SteerDamping = 5;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "SteerFactor"),
         UI_FloatRange(minValue = 0.1f, maxValue = 20f, stepIncrement = .1f, scene = UI_Scene.All)] public float
            SteerMult = 10;

        #endregion

        #region KSP ACTIONS

        [KSPAction("Start Guidance")]
        public void AgStartGuidance(KSPActionParam param)
        {
            StartGuidance();
        }

        [KSPAction("Fire Missile")]
        public void AgFire(KSPActionParam param)
        {
            FireMissile();
            if (BDArmorySettings.Instance.ActiveWeaponManager != null)
                BDArmorySettings.Instance.ActiveWeaponManager.UpdateList();
        }

        #endregion

        #region KSP EVENTS

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Fire Missile", active = true)]
        public void FireMissile()
        {
            BDATargetManager.FiredMissiles.Add(this);

            foreach (var wpm in vessel.FindPartModulesImplementing<MissileFire>())
            {
                Team = wpm.team;
                break;
            }

            SourceVessel = vessel;

            SetTargeting();


            Jettison();

            vessel.vesselName = GetShortName();
            vessel.vesselType = VesselType.Station;

            _firedTime = DateTime.Now;
            _firedTriggered = true;
        }

        private void SetTargeting()
        {
        }

        public Vector3 StartDirection { get; set; }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Start Guidance", active = true)]
        public void StartGuidance()
        {
            if (vessel.targetObject != null && vessel.targetObject.GetVessel() != null)
            {
                _targetVessel = vessel.targetObject.GetVessel();
            }
            else if (_parentVessel != null && _parentVessel.targetObject != null &&
                     _parentVessel.targetObject.GetVessel() != null)
            {
                _targetVessel = _parentVessel.targetObject.GetVessel();
            }
            else
            {
                return;
            }

            if (_targetVessel != null)
            {
                _readyForGuidance = true;
            }
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Guidance Mode", active = true)]
        public void SwitchGuidanceMode()
        {
            GuidanceMode++;
            if (GuidanceMode > 3)
            {
                GuidanceMode = 1;
            }

            RefreshGuidanceMode();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Targeting Mode", active = true)]
        public void SwitchTargetingMode()
        {
            var targetingModes = Enum.GetNames(typeof(TargetingModes));

            var currentIndex = targetingModes.IndexOf(TargetingMode.ToString());

            if (currentIndex < targetingModes.Length - 1)
            {
                TargetingMode = (TargetingModes) Enum.Parse(typeof(TargetingModes), targetingModes[currentIndex + 1]);
            }
            else
            {
                TargetingMode = (TargetingModes) Enum.Parse(typeof(TargetingModes), targetingModes[0]);
            }

            RefreshTargetingMode();
        }


        [KSPEvent(guiActive = true, guiActiveEditor = false, active = true, guiName = "Jettison")]
        public void Jettison()
        {
            if (_targetDecoupler == null || !_targetDecoupler || !(_targetDecoupler is IStageSeparator)) return;
            if (_targetDecoupler is ModuleDecouple)
            {
                (_targetDecoupler as ModuleDecouple).Decouple();
            }
            else
            {
                (_targetDecoupler as ModuleAnchoredDecoupler).Decouple();
            }

            if (BDArmorySettings.Instance.ActiveWeaponManager != null)
                BDArmorySettings.Instance.ActiveWeaponManager.UpdateList();
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Hide Weapon Name UI", active = false)]
        public void HideUI()
        {
            WeaponNameWindow.HideGUI();
            UpdateMenus(false);
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Set Weapon Name UI", active = false)]
        public void ShowUI()
        {
            WeaponNameWindow.ShowGUI(this);
            UpdateMenus(true);
        }

        #endregion
    }


    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class WeaponNameWindow : MonoBehaviour
    {
        internal static EventVoid OnActionGroupEditorOpened = new EventVoid("OnActionGroupEditorOpened");
        internal static EventVoid OnActionGroupEditorClosed = new EventVoid("OnActionGroupEditorClosed");

        private static GUIStyle unchanged;
        private static GUIStyle changed;
        private static GUIStyle greyed;
        private static GUIStyle overfull;

        private static WeaponNameWindow instance;
        private static Vector3 mousePos = Vector3.zero;

        private bool ActionGroupMode;

        private Rect guiWindowRect = new Rect(0, 0, 0, 0);

        private BDModularGuidance missile_module;

        [KSPField] public int offsetGUIPos = -1;

        private Vector2 scrollPos;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Show Weapon Name Editor"),
         UI_Toggle(enabledText = "Weapon Name GUI", disabledText = "GUI")] [NonSerialized] public bool showRFGUI;

        private bool styleSetup;

        private string txtName = string.Empty;

        public static void HideGUI()
        {
            if (instance != null)
            {
                instance.missile_module.WeaponName = instance.missile_module.shortName;
                instance.missile_module = null;
                instance.UpdateGUIState();
            }
            var editor = EditorLogic.fetch;
            if (editor != null)
                editor.Unlock("BD_MN_GUILock");
        }

        public static void ShowGUI(BDModularGuidance missile_module)
        {
            if (instance != null)
            {
                instance.missile_module = missile_module;
                instance.UpdateGUIState();
            }
        }

        private void UpdateGUIState()
        {
            enabled = missile_module != null;
            var editor = EditorLogic.fetch;
            if (!enabled && editor != null)
                editor.Unlock("BD_MN_GUILock");
        }

        private IEnumerator<YieldInstruction> CheckActionGroupEditor()
        {
            while (EditorLogic.fetch == null)
            {
                yield return null;
            }
            var editor = EditorLogic.fetch;
            while (EditorLogic.fetch != null)
            {
                if (editor.editorScreen == EditorScreen.Actions)
                {
                    if (!ActionGroupMode)
                    {
                        HideGUI();
                        OnActionGroupEditorOpened.Fire();
                    }
                    var age = EditorActionGroups.Instance;
                    if (missile_module && !age.GetSelectedParts().Contains(missile_module.part))
                    {
                        HideGUI();
                    }
                    ActionGroupMode = true;
                }
                else
                {
                    if (ActionGroupMode)
                    {
                        HideGUI();
                        OnActionGroupEditorClosed.Fire();
                    }
                    ActionGroupMode = false;
                }
                yield return null;
            }
        }

        private void Awake()
        {
            enabled = false;
            instance = this;
            StartCoroutine(CheckActionGroupEditor());
        }

        private void OnDestroy()
        {
            instance = null;
        }

        public void OnGUI()
        {
            if (!styleSetup)
            {
                styleSetup = true;
                Styles.InitStyles();
            }

            var editor = EditorLogic.fetch;
            if (!HighLogic.LoadedSceneIsEditor || !editor)
            {
                return;
            }
            var cursorInGUI = false; // nicked the locking code from Ferram
            mousePos = Input.mousePosition; //Mouse location; based on Kerbal Engineer Redux code
            mousePos.y = Screen.height - mousePos.y;

            var posMult = 0;
            if (offsetGUIPos != -1)
            {
                posMult = offsetGUIPos;
            }
            if (ActionGroupMode)
            {
                if (guiWindowRect.width == 0)
                {
                    guiWindowRect = new Rect(430*posMult, 365, 438, 50);
                }
                new Rect(guiWindowRect.xMin + 440, mousePos.y - 5, 300, 20);
            }
            else
            {
                if (guiWindowRect.width == 0)
                {
                    //guiWindowRect = new Rect(Screen.width - 8 - 430 * (posMult + 1), 365, 438, (Screen.height - 365));
                    guiWindowRect = new Rect(Screen.width - 8 - 430*(posMult + 1), 365, 438, 50);
                }
                new Rect(guiWindowRect.xMin - (230 - 8), mousePos.y - 5, 220, 20);
            }
            cursorInGUI = guiWindowRect.Contains(mousePos);
            if (cursorInGUI)
            {
                editor.Lock(false, false, false, "BD_MN_GUILock");
                if (EditorTooltip.Instance != null)
                    EditorTooltip.Instance.HideToolTip();
            }
            else
            {
                editor.Unlock("BD_MN_GUILock");
            }
            guiWindowRect = GUILayout.Window(GetInstanceID(), guiWindowRect, GUIWindow, "Weapon Name GUI",
                Styles.styleEditorPanel);
        }

        public void GUIWindow(int windowID)
        {
            InitializeStyles();

            GUILayout.BeginVertical();
            GUILayout.Space(20);

            GUILayout.BeginHorizontal();

            GUILayout.Label("Weapon Name: ");


            txtName = GUILayout.TextField(txtName);


            if (GUILayout.Button("Save & Close"))
            {
                missile_module.WeaponName = txtName;
                missile_module.shortName = txtName;
                instance.missile_module.HideUI();
            }

            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        private static void InitializeStyles()
        {
            if (unchanged == null)
            {
                if (GUI.skin == null)
                {
                    unchanged = new GUIStyle();
                    changed = new GUIStyle();
                    greyed = new GUIStyle();
                    overfull = new GUIStyle();
                }
                else
                {
                    unchanged = new GUIStyle(GUI.skin.textField);
                    changed = new GUIStyle(GUI.skin.textField);
                    greyed = new GUIStyle(GUI.skin.textField);
                    overfull = new GUIStyle(GUI.skin.label);
                }

                unchanged.normal.textColor = Color.white;
                unchanged.active.textColor = Color.white;
                unchanged.focused.textColor = Color.white;
                unchanged.hover.textColor = Color.white;

                changed.normal.textColor = Color.yellow;
                changed.active.textColor = Color.yellow;
                changed.focused.textColor = Color.yellow;
                changed.hover.textColor = Color.yellow;

                greyed.normal.textColor = Color.gray;

                overfull.normal.textColor = Color.red;
            }
        }
    }

    internal class Styles
    {
        // Base styles
        public static GUIStyle styleEditorTooltip;
        public static GUIStyle styleEditorPanel;


        /// <summary>
        ///     This one sets up the styles we use
        /// </summary>
        internal static void InitStyles()
        {
            styleEditorTooltip = new GUIStyle();
            styleEditorTooltip.name = "Tooltip";
            styleEditorTooltip.fontSize = 12;
            styleEditorTooltip.normal.textColor = new Color32(207, 207, 207, 255);
            styleEditorTooltip.stretchHeight = true;
            styleEditorTooltip.wordWrap = true;
            styleEditorTooltip.normal.background = CreateColorPixel(new Color32(7, 54, 66, 200));
            styleEditorTooltip.border = new RectOffset(3, 3, 3, 3);
            styleEditorTooltip.padding = new RectOffset(4, 4, 6, 4);
            styleEditorTooltip.alignment = TextAnchor.MiddleLeft;

            styleEditorPanel = new GUIStyle();
            styleEditorPanel.normal.background = CreateColorPixel(new Color32(7, 54, 66, 200));
            styleEditorPanel.border = new RectOffset(27, 27, 27, 27);
            styleEditorPanel.padding = new RectOffset(10, 10, 10, 10);
            styleEditorPanel.normal.textColor = new Color32(147, 161, 161, 255);
            styleEditorPanel.fontSize = 12;
        }


        /// <summary>
        ///     Creates a 1x1 texture
        /// </summary>
        /// <param name="Background">Color of the texture</param>
        /// <returns></returns>
        internal static Texture2D CreateColorPixel(Color32 Background)
        {
            var retTex = new Texture2D(1, 1);
            retTex.SetPixel(0, 0, Background);
            retTex.Apply();
            return retTex;
        }
    }
}