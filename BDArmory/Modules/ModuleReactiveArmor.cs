using System;
using System.Collections.Generic;
using System.Linq;
using BDArmory.Core.Module;
using UnityEngine;

namespace BDArmory.Modules
{
    public class ModuleReactiveArmor : PartModule
    {

        //[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Damage Percentage Threshold"),
        // UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0f, maxValue = 100f, stepIncrement = 1f)]
        public float DAMAGEMODIFIER1 = 75;

        //[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Armor Percentage Threshold"),
        // UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0f, maxValue = 100f, stepIncrement = 1f)]
        public float ARMORMODIFIER1 = 75;

        //[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Damage Modifer [Stage 2]"),
        // UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0f, maxValue = 100f, stepIncrement = 1f)]
        public float DAMAGEMODIFIER2 = 35;

        //[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Armor Modifer [Stage 2]"),
        // UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0f, maxValue = 100f, stepIncrement = 1f)]
        public float ARMORMODIFIER2 = 35;

        public bool underAttack = false;
        private float partHPmax = 0.0f;
        private float partHPtotal = 0.0f;
        private float partArmorMax = 0.0f;
        private float partArmorTotal = 0.0f;
        private double stage = 1;
        private bool pauseRoutine = false;

        private float HP = 0.0f;
        private bool partCheck = true;

        private HitpointTracker hp;
        private HitpointTracker GetHP()
        {
            HitpointTracker hp = null;

            hp = part.FindModuleImplementing<HitpointTracker>();

            return hp;
        }


        public override void OnStart(StartState state)
        {
            initializeData();

            useTextureAll(false);

            if (HighLogic.LoadedSceneIsFlight)
            {
                Setup();
            }
            base.OnStart(state);
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;
            if (stage == 1)
            {
                CheckPart();
            }
        }

        private void ScreenMsg(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 0.005f, ScreenMessageStyle.UPPER_RIGHT));
        }

        private void Setup()
        {
            hp = GetHP();

            partHPmax = hp.maxHitPoints;
            partHPtotal = hp.Hitpoints;
            partArmorMax = hp.ArmorThickness;
            partArmorTotal = hp.Armor;
        }

        public void CheckPart()
        {
            hp = GetHP();
            partHPtotal = hp.Hitpoints;
            partArmorTotal = hp.Armor;

            if (stage != 1 || (!(partHPtotal <= partHPmax * DAMAGEMODIFIER1 / 100) &&
                               !(partArmorTotal <= partArmorMax * ARMORMODIFIER1 / 100))) return;
            stage = 2;
            hp.Armor = ARMORMODIFIER2 * partArmorMax / 100;
            hp.ArmorThickness = hp.Armor;
            hp.Hitpoints = DAMAGEMODIFIER2 * partHPmax / 100;
            hp.maxHitPoints = hp.Hitpoints;

            partHPmax = hp.maxHitPoints;
            partHPtotal = hp.Hitpoints;
            partArmorMax = hp.ArmorThickness;
            partArmorTotal = hp.Armor;

            nextTextureEvent();
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [KSPField]
        public string currentTextureName = string.Empty;
        [KSPField]
        public string textureRootFolder = string.Empty;
        [KSPField]
        public string objectNames = string.Empty;
        [KSPField]
        public string textureNames = string.Empty;
        [KSPField]
        public string mapNames = string.Empty;
        [KSPField]
        public string textureDisplayNames = "Default";
        [KSPField(isPersistant = true)]
        public int selectedTexture = 0;
        [KSPField(isPersistant = true)]
        public string selectedTextureURL = string.Empty;
        [KSPField(isPersistant = true)]
        public string selectedMapURL = string.Empty;
        [KSPField]
        public string additionalMapType = "_BumpMap";
        [KSPField]
        public bool mapIsNormal = true;

        private List<Transform> targetObjectTransforms = new List<Transform>();
        private List<List<Material>> targetMats = new List<List<Material>>();
        private List<String> texList = new List<string>();
        private List<String> mapList = new List<string>();
        private List<String> objectList = new List<string>();
        private List<String> textureDisplayList = new List<string>();

        private bool initialized = false;

        List<Transform> ListChildren(Transform a)
        {
            List<Transform> childList = new List<Transform>();
            foreach (Transform b in a)
            {
                childList.Add(b);
                childList.AddRange(ListChildren(b));
            }
            return childList;
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Next Texture")]
        public void nextTextureEvent()
        {
            selectedTexture++;
            if (selectedTexture >= texList.Count && selectedTexture >= mapList.Count)
                selectedTexture = 0;
            useTextureAll(true);
        }

        public void useTextureAll(bool calledByPlayer)
        {
            applyTexToPart(calledByPlayer);
        }

        private void applyTexToPart(bool calledByPlayer)
        {
            initializeData();
            foreach (List<Material> matList in targetMats)
            {
                foreach (Material mat in matList)
                {
                    useTextureOrMap(mat);
                }
            }
        }

        public void useTextureOrMap(Material targetMat)
        {
            if (targetMat == null) return;
            useTexture(targetMat);

            useMap(targetMat);
        }

        private void useMap(Material targetMat)
        {
            if (mapList.Count <= selectedTexture) return;
            if (GameDatabase.Instance.ExistsTexture(mapList[selectedTexture]))
            {
                targetMat.SetTexture(additionalMapType, GameDatabase.Instance.GetTexture(mapList[selectedTexture], mapIsNormal));
                selectedMapURL = mapList[selectedTexture];

                if (selectedTexture < textureDisplayList.Count && texList.Count == 0)
                {
                    currentTextureName = textureDisplayList[selectedTexture];
                }
            }
            if (mapList.Count > selectedTexture) ; // why is this check here? will never happen.
            else
            {
                for (int i = 0; i < mapList.Count; i++) ;
            }
        }

        private void useTexture(Material targetMat)
        {
            if (texList.Count <= selectedTexture) return;
            if (!GameDatabase.Instance.ExistsTexture(texList[selectedTexture])) return;
            targetMat.mainTexture = GameDatabase.Instance.GetTexture(texList[selectedTexture], false);
            selectedTextureURL = texList[selectedTexture];

            currentTextureName = selectedTexture > textureDisplayList.Count - 1 ? 
                getTextureDisplayName(texList[selectedTexture]) : 
                textureDisplayList[selectedTexture];
        }

        private string getTextureDisplayName(string longName)
        {
            string[] splitString = longName.Split('/');
            return splitString[splitString.Length - 1];
        }

        private void initializeData()
        {
            if (initialized) return;
            objectList = parseNames(objectNames, true);
            texList = parseNames(textureNames, true, true, textureRootFolder);
            mapList = parseNames(mapNames, true, true, textureRootFolder);
            textureDisplayList = parseNames(textureDisplayNames);

            foreach (string targetObjectName in objectList)
            {
                Transform[] targetObjectTransformArray = part.FindModelTransforms(targetObjectName);
                List<Material> matList = new List<Material>();
                foreach (Transform t in targetObjectTransformArray)
                {
                    if (t == null || t.gameObject.GetComponent<Renderer>() == null) continue;
                    Material targetMat = t.gameObject.GetComponent<Renderer>().material;
                    if (targetMat == null) continue;
                    if (!matList.Contains(targetMat))
                    {
                        matList.Add(targetMat);
                    }
                }
                targetMats.Add(matList);
            }
            initialized = true;
        }

        /////////////////////////////////////////////////////////////////////

        public static List<string> parseNames(string names)
        {
            return parseNames(names, false, true, string.Empty);
        }

        public static List<string> parseNames(string names, bool replaceBackslashErrors)
        {
            return parseNames(names, replaceBackslashErrors, true, string.Empty);
        }

        public static List<string> parseNames(string names, bool replaceBackslashErrors, bool trimWhiteSpace, string prefix)
        {
            List<string> source = names.Split(';').ToList<string>();
            for (int i = source.Count - 1; i >= 0; i--)
            {
                if (source[i] == string.Empty)
                {
                    source.RemoveAt(i);
                }
            }
            if (trimWhiteSpace)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    source[i] = source[i].Trim(' ');
                }
            }
            if (prefix != string.Empty)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    source[i] = prefix + source[i];
                }
            }
            if (replaceBackslashErrors)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    source[i] = source[i].Replace('\\', '/');
                }
            }
            return source.ToList<string>();
        }

        public static List<int> parseIntegers(string stringOfInts)
        {
            List<int> newIntList = new List<int>();
            string[] valueArray = stringOfInts.Split(';');
            for (int i = 0; i < valueArray.Length; i++)
            {
                int newValue = 0;
                if (int.TryParse(valueArray[i], out newValue))
                {
                    newIntList.Add(newValue);
                }
                else
                {
                    Debug.Log("invalid integer: " + valueArray[i]);
                }
            }
            return newIntList;
        }
    }
}