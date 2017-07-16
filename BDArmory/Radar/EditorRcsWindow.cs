using System.Collections;
using System.Collections.Generic;
using BDArmory.CounterMeasure;
using BDArmory.Misc;
using BDArmory.Parts;
using BDArmory.Radar;
using KSP.UI.Screens;
using UnityEngine;
using BDArmory.UI;

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


        void Awake()
        {
        }

        void Start()
        {
            Instance = this;
            AddToolbarButton();
        }

        private void OnDestroy()
        {
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
        }

        public void HideToolbarGUI()
        {
            showRcsWindow = false;
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
            GUI.Label(new Rect(430, 40, 200, 20), "Dorsal",  HighLogic.Skin.box);

            GUI.DragWindow();
        }


    } //EditorRCsWindow
}
