using System.Collections;
using System.Collections.Generic;
using BDArmory.Modules;
using BDArmory.Misc;
using UnityEngine;

namespace BDArmory.UI
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LoadedVesselSwitcher : MonoBehaviour
    {
        private readonly float _buttonGap = 1;
        private readonly float _buttonHeight = 20;

        private int _guiCheckIndex;
        public LoadedVesselSwitcher Instance;
        private readonly float _margin = 5;

        private bool _ready;
        private bool _showGui;
        private bool _teamSwitchDirty;
        private readonly float _titleHeight = 30;
        private float updateTimer = 0;

        //gui params
        private float _windowHeight; //auto adjusting
        private readonly float _windowWidth = 250;

        private SortedList<string, List<MissileFire>> weaponManagers = new SortedList<string, List<MissileFire>>();

        private MissileFire _wmToSwitchTeam;

        private void Awake()
        {
            if (Instance)
                Destroy(this);
            else
                Instance = this;
        }

        private void Start()
        {
            UpdateList();
            GameEvents.onVesselCreate.Add(VesselEventUpdate);
            GameEvents.onVesselDestroy.Add(VesselEventUpdate);
            GameEvents.onVesselGoOffRails.Add(VesselEventUpdate);
            GameEvents.onVesselGoOnRails.Add(VesselEventUpdate);
            MissileFire.OnChangeTeam += MissileFireOnToggleTeam;

            _ready = false;
            StartCoroutine(WaitForBdaSettings());

            // TEST
            FloatingOrigin.fetch.threshold = 20000; //20km
            FloatingOrigin.fetch.thresholdSqr = 20000*20000; //20km
            Debug.Log($"FLOATINGORIGIN: threshold is {FloatingOrigin.fetch.threshold}");

            //BDArmorySetup.WindowRectVesselSwitcher = new Rect(10, Screen.height / 6f, _windowWidth, 10);
        }

        private void OnDestroy()
        {
                GameEvents.onVesselCreate.Remove(VesselEventUpdate);
                GameEvents.onVesselDestroy.Remove(VesselEventUpdate);
                GameEvents.onVesselGoOffRails.Remove(VesselEventUpdate);
                GameEvents.onVesselGoOnRails.Remove(VesselEventUpdate);
                MissileFire.OnChangeTeam -= MissileFireOnToggleTeam;

                _ready = false;

            // TEST
            Debug.Log($"FLOATINGORIGIN: threshold is {FloatingOrigin.fetch.threshold}");
        }

        private IEnumerator WaitForBdaSettings()
        {
            while (BDArmorySetup.Instance == null)
                yield return null;

            _ready = true;
            BDArmorySetup.Instance.hasVS = true;
            _guiCheckIndex = Misc.Misc.RegisterGUIRect(new Rect());
        }

        private void MissileFireOnToggleTeam(MissileFire wm, BDTeam team)
        {
            if (_showGui)
                UpdateList();
        }

        private void VesselEventUpdate(Vessel v)
        {
            if (_showGui)
                UpdateList();
        }

        private void Update()
        {
            if (_ready)
            {
                if (BDArmorySetup.Instance.showVSGUI != _showGui)
                {
                    updateTimer -= Time.fixedDeltaTime;
                    _showGui = BDArmorySetup.Instance.showVSGUI;
                    if (_showGui && updateTimer < 0)
                    {
                        UpdateList();
                        updateTimer = 0.5f;    //next update in half a sec only
                    }
                }

                if (_showGui)
                {                    
                    Hotkeys();
                }
            }
        }

        private void Hotkeys()
        {
            if (BDInputUtils.GetKeyDown(BDInputSettingsFields.VS_SWITCH_NEXT))
                SwitchToNextVessel();
            if (BDInputUtils.GetKeyDown(BDInputSettingsFields.VS_SWITCH_PREV))
                SwitchToPreviousVessel();
        }

        private void UpdateList()
        {
            weaponManagers.Clear();

            List<Vessel>.Enumerator v = FlightGlobals.Vessels.GetEnumerator();
            while (v.MoveNext())
            {
                if (v.Current == null || !v.Current.loaded || v.Current.packed)
                    continue;
                var wm = v.Current.FindPartModuleImplementing<MissileFire>();
                if (weaponManagers.TryGetValue(wm.Team.Name, out var teamManagers))
                    teamManagers.Add(wm);
                else
                    weaponManagers.Add(wm.Team.Name, new List<MissileFire> { wm });
            }
            v.Dispose();
        }

        private void OnGUI()
        {
            if (_ready)
            {
                if (_showGui && BDArmorySetup.GAME_UI_ENABLED)
                {
                    SetNewHeight(_windowHeight);
                    // this Rect initialization ensures any save issues with height or width of the window are resolved
                    BDArmorySetup.WindowRectVesselSwitcher = new Rect(BDArmorySetup.WindowRectVesselSwitcher.x, BDArmorySetup.WindowRectVesselSwitcher.y, _windowWidth, _windowHeight);
                    BDArmorySetup.WindowRectVesselSwitcher = GUI.Window(10293444, BDArmorySetup.WindowRectVesselSwitcher, WindowVesselSwitcher, "BDA Vessel Switcher",
                        BDArmorySetup.BDGuiSkin.window);
                    Misc.Misc.UpdateGUIRect(BDArmorySetup.WindowRectVesselSwitcher, _guiCheckIndex);
                }
                else
                {
                    Misc.Misc.UpdateGUIRect(new Rect(), _guiCheckIndex);
                }

                if (_teamSwitchDirty)
                {
                    if (_wmToSwitchTeam)
                        _wmToSwitchTeam.NextTeam();
                    _teamSwitchDirty = false;
                    _wmToSwitchTeam = null;
                }
            }
        }

        private void SetNewHeight(float windowHeight)
        {
            BDArmorySetup.WindowRectVesselSwitcher.height = windowHeight;
        }

        private void WindowVesselSwitcher(int id)
        {
            GUI.DragWindow(new Rect(0, 0, _windowWidth - _buttonHeight - 4, _titleHeight));
            if (GUI.Button(new Rect(_windowWidth - _buttonHeight - 4, 4, _buttonHeight, _buttonHeight), "X",
                BDArmorySetup.BDGuiSkin.button))
            {
                BDArmorySetup.Instance.showVSGUI = false;
                return;
            }
            float height = _titleHeight;
            float vesselButtonWidth = _windowWidth - 2 * _margin - 3 * _buttonHeight;

            using (var teamManagers = weaponManagers.GetEnumerator())
                while (teamManagers.MoveNext())
                {
                    height += _margin;
                    GUI.Label(new Rect(_margin, height, _windowWidth - 2 * _margin, _buttonHeight), $"{teamManagers.Current.Key}:", BDArmorySetup.BDGuiSkin.label);
                    height += _buttonHeight;

                    using (var wm = teamManagers.Current.Value.GetEnumerator())
                        while (wm.MoveNext())
                        {
                            if (wm.Current == null) continue;

                            Rect buttonRect = new Rect(_margin, height, vesselButtonWidth, _buttonHeight);
                            GUIStyle vButtonStyle = wm.Current.vessel.isActiveVessel ? BDArmorySetup.BDGuiSkin.box : BDArmorySetup.BDGuiSkin.button;
                            string status = UpdateVesselStatus(wm.Current, vButtonStyle);

                            if (GUI.Button(buttonRect, status + wm.Current.vessel.GetName(), vButtonStyle))
                                ForceSwitchVessel(wm.Current.vessel);

                            //guard toggle
                            GUIStyle guardStyle = wm.Current.guardMode ? BDArmorySetup.BDGuiSkin.box : BDArmorySetup.BDGuiSkin.button;
                            Rect guardButtonRect = new Rect(_margin + vesselButtonWidth, height, _buttonHeight, _buttonHeight);
                            if (GUI.Button(guardButtonRect, "G", guardStyle))
                                wm.Current.ToggleGuardMode();

                            //AI toggle
                            if (wm.Current.AI != null)
                            {
                                GUIStyle aiStyle = wm.Current.AI.pilotEnabled ? BDArmorySetup.BDGuiSkin.box : BDArmorySetup.BDGuiSkin.button;
                                Rect aiButtonRect = new Rect(_margin + vesselButtonWidth + _buttonHeight, height, _buttonHeight,
                                    _buttonHeight);
                                if (GUI.Button(aiButtonRect, "P", aiStyle))
                                    wm.Current.AI.TogglePilot();
                            }

                            //team toggle
                            Rect teamButtonRect = new Rect(_margin + vesselButtonWidth + _buttonHeight * 2, height,
                                _buttonHeight, _buttonHeight);
                            if (GUI.Button(teamButtonRect, "T", BDArmorySetup.BDGuiSkin.button))
                            {
                                _wmToSwitchTeam = wm.Current;
                                _teamSwitchDirty = true;
                            }

                            height += _buttonHeight + _buttonGap;
                        }
                }

            height += _margin;
            _windowHeight = height;
            BDGUIUtils.RepositionWindow(ref BDArmorySetup.WindowRectVesselSwitcher);
        }

        private string UpdateVesselStatus(MissileFire wm, GUIStyle vButtonStyle)
        {
            string status = "";
            if (wm.vessel.LandedOrSplashed)
            {
                if (wm.vessel.Landed)
                    status = "(Landed)";
                else
                    status = "(Splashed)";
                vButtonStyle.fontStyle = FontStyle.Italic;
            }
            else
            {
                vButtonStyle.fontStyle = FontStyle.Normal;
            }
            return status;
        }

        private void SwitchToNextVessel()
        {
            if (weaponManagers.Count == 0) return;

            bool switchNext = false;

            using (var teamManagers = weaponManagers.GetEnumerator())
                while (teamManagers.MoveNext())
                    using (var wm = teamManagers.Current.Value.GetEnumerator())
                        while (wm.MoveNext())
                        {
                            if (wm.Current.vessel.isActiveVessel)
                                switchNext = true;
                            else if (switchNext)
                            {
                                ForceSwitchVessel(wm.Current.vessel);
                                return;
                            }
                        }
            var firstVessel = weaponManagers.Values[0][0].vessel;
            if (!firstVessel.isActiveVessel)
                ForceSwitchVessel(firstVessel);
        }

        private void SwitchToPreviousVessel()
        {
            if (weaponManagers.Count == 0) return;

            Vessel previousVessel = weaponManagers.Values[weaponManagers.Count][weaponManagers.Values[weaponManagers.Count].Count].vessel;

            using (var teamManagers = weaponManagers.GetEnumerator())
                while (teamManagers.MoveNext())
                    using (var wm = teamManagers.Current.Value.GetEnumerator())
                        while (wm.MoveNext())
                        {
                            if (wm.Current.vessel.isActiveVessel)
                                ForceSwitchVessel(previousVessel);
                            previousVessel = wm.Current.vessel;
                        }
            if (!previousVessel.isActiveVessel)
                ForceSwitchVessel(previousVessel);
        }
        
        // Extracted method, so we dont have to call these two lines everywhere
        private void ForceSwitchVessel(Vessel v)
        {
            FlightGlobals.ForceSetActiveVessel(v);
            FlightInputHandler.ResumeVesselCtrlState(v);
        }

    }
}