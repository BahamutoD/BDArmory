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
        private static int radarResolution = 128;
        private static RenderTexture rcsRenderingFrontal;
        private static RenderTexture rcsRenderingLateral;
        private static RenderTexture rcsRenderingVentral;
        private static Texture2D drawTextureFrontal;
        private static Texture2D drawTextureLateral;
        private static Texture2D drawTextureVentral;
        private float rcsFrontal;
        private float rcsLateral;
        private float rcsVentral;
        private static Camera radarCam;
        private const float CONSTANT_RCS_FACTOR = 16.0f;


        void Awake()
        {
        }

        void Start()
        {
            Instance = this;
            AddToolbarButton();

            if (!rcsRenderingFrontal)
            {
                rcsRenderingFrontal = new RenderTexture(radarResolution, radarResolution, 16);
                rcsRenderingLateral = new RenderTexture(radarResolution, radarResolution, 16);
                rcsRenderingVentral = new RenderTexture(radarResolution, radarResolution, 16);
                drawTextureFrontal = new Texture2D(radarResolution, radarResolution, TextureFormat.RGB24, false);
                drawTextureLateral = new Texture2D(radarResolution, radarResolution, TextureFormat.RGB24, false);
                drawTextureVentral = new Texture2D(radarResolution, radarResolution, TextureFormat.RGB24, false);

                //set up camera
                radarCam = (new GameObject("RadarCamera")).AddComponent<Camera>();
                radarCam.enabled = false;
                radarCam.clearFlags = CameraClearFlags.SolidColor;
                radarCam.backgroundColor = Color.black;
                radarCam.cullingMask = 1 << 0;   // only layer 0 active, see: http://wiki.kerbalspaceprogram.com/wiki/API:Layers
            }
        }

        private void OnDestroy()
        {
            if (toolbarButton)
            {
                ApplicationLauncher.Instance.RemoveModApplication(toolbarButton);
                toolbarButton = null;
            }

            if (rcsRenderingFrontal)
            {
                RenderTexture.Destroy(rcsRenderingFrontal);
                RenderTexture.Destroy(rcsRenderingLateral);
                RenderTexture.Destroy(rcsRenderingVentral);
                Texture2D.Destroy(drawTextureFrontal);
                Texture2D.Destroy(drawTextureLateral);
                Texture2D.Destroy(drawTextureVentral);
                GameObject.Destroy(radarCam);
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

            GUI.DrawTexture(new Rect(10, 70, 200, 200), drawTextureFrontal, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(220, 70, 200, 200), drawTextureLateral, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(430, 70, 200, 200), drawTextureVentral, ScaleMode.StretchToFill);


            GUI.Label(new Rect(10, 275, 200, 20), string.Format("{0:0.00}", rcsFrontal) + " m^2", HighLogic.Skin.label);
            GUI.Label(new Rect(220, 275, 200, 20), string.Format("{0:0.00}", rcsLateral) + " m^2", HighLogic.Skin.label);
            GUI.Label(new Rect(430, 275, 200, 20), string.Format("{0:0.00}", rcsVentral) + " m^2", HighLogic.Skin.label);

            float rcsTotal = (rcsFrontal + rcsLateral + rcsVentral) / 3f;
            GUIStyle style = HighLogic.Skin.label;
            style.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(10, 300, 600, 20), "Total radar cross section for vessel: " + string.Format("{0:0.00} (without ECM/countermeasures)", rcsTotal) + " m^2" , style);

            GUI.DragWindow();
        }


        void takeRadarSnapshot()
        {
            float radarDistance = 1000f;
            float radarFOV = 2.0f;
            float distanceToShip;

            if (EditorLogic.RootPart is null)
                return;

            Bounds shipbounds = CalcShipBounds(EditorLogic.fetch.ship);
            Debug.Log("SHIPBOUNDS: " + shipbounds.ToString());
            Debug.Log("SHIPSIZE: " + shipbounds.size + ", MAGNITUDE: "+ shipbounds.size.magnitude);

            // pass: frontal
            radarCam.transform.position = shipbounds.center + EditorLogic.RootPart.transform.up * radarDistance;
            radarCam.transform.LookAt(shipbounds.center);
            distanceToShip = Vector3.Distance(radarCam.transform.position, shipbounds.center);
            radarCam.nearClipPlane = distanceToShip - 200;
            radarCam.farClipPlane = distanceToShip + 200;
            radarCam.fieldOfView = radarFOV;
            radarCam.targetTexture = rcsRenderingFrontal;
            RenderTexture.active = rcsRenderingFrontal;
            Shader.SetGlobalVector("_LIGHTDIR", -EditorLogic.RootPart.transform.up);
            radarCam.RenderWithShader(BDAShaderLoader.RCSShader, string.Empty);
            drawTextureFrontal.ReadPixels(new Rect(0, 0, radarResolution, radarResolution), 0, 0);
            drawTextureFrontal.Apply();

            // pass: lateral
            radarCam.transform.position = shipbounds.center + EditorLogic.RootPart.transform.right * radarDistance;
            radarCam.transform.LookAt(shipbounds.center);
            distanceToShip = Vector3.Distance(radarCam.transform.position, shipbounds.center);
            radarCam.nearClipPlane = distanceToShip - 200;
            radarCam.farClipPlane = distanceToShip + 200;
            radarCam.fieldOfView = radarFOV;
            radarCam.targetTexture = rcsRenderingLateral;
            RenderTexture.active = rcsRenderingLateral;
            Shader.SetGlobalVector("_LIGHTDIR", -EditorLogic.RootPart.transform.right);
            radarCam.RenderWithShader(BDAShaderLoader.RCSShader, string.Empty);
            drawTextureLateral.ReadPixels(new Rect(0, 0, radarResolution, radarResolution), 0, 0);
            drawTextureLateral.Apply();

            // pass: Ventral
            radarCam.transform.position = shipbounds.center + EditorLogic.RootPart.transform.forward * radarDistance;
            radarCam.transform.LookAt(shipbounds.center);
            distanceToShip = Vector3.Distance(radarCam.transform.position, shipbounds.center);
            radarCam.nearClipPlane = distanceToShip - 200;
            radarCam.farClipPlane = distanceToShip + 200;
            radarCam.fieldOfView = radarFOV;
            radarCam.targetTexture = rcsRenderingVentral;
            RenderTexture.active = rcsRenderingVentral;
            Shader.SetGlobalVector("_LIGHTDIR", -EditorLogic.RootPart.transform.forward);
            radarCam.RenderWithShader(BDAShaderLoader.RCSShader, string.Empty);
            drawTextureVentral.ReadPixels(new Rect(0, 0, radarResolution, radarResolution), 0, 0);
            drawTextureVentral.Apply();

            // Count pixel colors to determine radar returns
            rcsFrontal = 0;
            rcsLateral = 0;
            rcsVentral = 0;
            for (int x = 0; x < radarResolution; x++)
            {
                for (int y = 0; y < radarResolution; y++)
                {
                    rcsFrontal += drawTextureFrontal.GetPixel(x, y).maxColorComponent;
                    rcsLateral += drawTextureLateral.GetPixel(x, y).maxColorComponent;
                    rcsVentral += drawTextureVentral.GetPixel(x, y).maxColorComponent;
                }
            }

            // normalize rcs value, so that the structural 1x1 panel facing the radar exactly gives a return of 1 m^2:
            rcsFrontal /= CONSTANT_RCS_FACTOR;
            rcsLateral /= CONSTANT_RCS_FACTOR;
            rcsVentral /= CONSTANT_RCS_FACTOR;

            takeSnapshot = false;
        }

        private Bounds CalcShipBounds(ShipConstruct ship)
        {
            Bounds result = new Bounds(ship.Parts[0].transform.position, Vector3.zero);
            foreach (var current in ship.Parts)
            {
                if (current.collider && !current.Modules.Contains("LaunchClamp"))
                {
                    result.Encapsulate(current.collider.bounds);
                }
            }
            return result;
        }

        public Vector3 GetShipSize(ShipConstruct ship)
        {
            return CalcShipBounds(ship).size;
        }


    } //EditorRCsWindow
}
