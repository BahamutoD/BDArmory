using System.Collections;
using System.Collections.Generic;
using BDArmory.CounterMeasure;
using BDArmory.Misc;
using BDArmory.Parts;
using BDArmory.Radar;
using KSP.UI.Screens;
using UnityEngine;
using BDArmory.UI;
using BDArmory.Shaders;
using System.Text;
using System;

namespace BDArmory.Radar
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class EditorRcsWindow : MonoBehaviour
    {

        public static EditorRcsWindow Instance = null;
        private ApplicationLauncherButton toolbarButton = null;

        private bool showRcsWindow = false;
        private string windowTitle = "BDArmory Radar Cross Section Analysis";
        private Rect windowRect = new Rect(300, 150, 650, 400);

        private bool takeSnapshot = false;


        void Awake()
        {
        }

        void Start()
        {
            Instance = this;
            AddToolbarButton();

            RadarUtils.SetupResources();
            GameEvents.onEditorShipModified.Add(OnEditorShipModifiedEvent);
        }

        private void OnEditorShipModifiedEvent(ShipConstruct data)
        {
            takeSnapshot = true;
        }

        private void OnDestroy()
        {
            GameEvents.onEditorShipModified.Remove(OnEditorShipModifiedEvent);
            RadarUtils.CleanupResources();

            if (toolbarButton)
            {
                ApplicationLauncher.Instance.RemoveModApplication(toolbarButton);
                toolbarButton = null;
            }
         }

        IEnumerator ToolbarButtonRoutine()
        {
            if (toolbarButton || (!HighLogic.LoadedSceneIsEditor)) yield break;
            while (!ApplicationLauncher.Ready)
            {
                yield return null;
            }

            AddToolbarButton();
        }

        void AddToolbarButton()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (toolbarButton is null)
                {
                    Texture buttonTexture = GameDatabase.Instance.GetTexture(BDArmorySettings.textureDir + "icon_rcs", false);
                    toolbarButton = ApplicationLauncher.Instance.AddModApplication(ShowToolbarGUI, HideToolbarGUI, Dummy, Dummy, Dummy, Dummy, ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB, buttonTexture);
                }
            }
        }

        public void ShowToolbarGUI()
        {
            showRcsWindow = true;
            takeSnapshot = true;
        }

        public void HideToolbarGUI()
        {
            showRcsWindow = false;
            takeSnapshot = false;
        }

        void Dummy()
        { }

        void OnGUI()
        {
            if (showRcsWindow)
            {
               windowRect = GUI.Window(this.GetInstanceID(), windowRect, RcsWindow, windowTitle, HighLogic.Skin.window);
               BDGUIUtils.UseMouseEventInRect(windowRect);
            }
        }

        void RcsWindow(int windowID)
        {
            GUI.Label(new Rect(10, 40, 200, 20), "Frontal", HighLogic.Skin.box);
            GUI.Label(new Rect(220, 40, 200, 20), "Lateral", HighLogic.Skin.box);
            GUI.Label(new Rect(430, 40, 200, 20), "Ventral",  HighLogic.Skin.box);

            if (takeSnapshot)
                takeRadarSnapshot();

            GUI.DrawTexture(new Rect(10, 70, 200, 200), RadarUtils.GetTextureFrontal, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(220, 70, 200, 200), RadarUtils.GetTextureLateral, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(430, 70, 200, 200), RadarUtils.GetTextureVentral, ScaleMode.StretchToFill);


            GUI.Label(new Rect(10, 275, 200, 20), string.Format("{0:0.00}", RadarUtils.rcsFrontal) + " m^2", HighLogic.Skin.label);
            GUI.Label(new Rect(220, 275, 200, 20), string.Format("{0:0.00}", RadarUtils.rcsLateral) + " m^2", HighLogic.Skin.label);
            GUI.Label(new Rect(430, 275, 200, 20), string.Format("{0:0.00}", RadarUtils.rcsVentral) + " m^2", HighLogic.Skin.label);

            GUIStyle style = HighLogic.Skin.label;
            style.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(10, 300, 600, 20), "Total radar cross section for vessel: " + string.Format("{0:0.00} m^2 (without ECM/countermeasures)", RadarUtils.rcsTotal) , style);

            GUI.DragWindow();
        }


        void takeRadarSnapshot()
        {
            if (EditorLogic.RootPart == null)
                return;

            // Encapsulate editor ShipConstruct into a vessel:
            Vessel v = new Vessel();
            v.parts = EditorLogic.fetch.ship.Parts;
            RadarUtils.RenderVesselRadarSnapshot(v, EditorLogic.RootPart.transform);  //first rendering for true RCS
            RadarUtils.RenderVesselRadarSnapshot(v, EditorLogic.RootPart.transform, true);  //second rendering for nice zoomed-in view
            takeSnapshot = false;
        }
         

    } //EditorRCsWindow
}
