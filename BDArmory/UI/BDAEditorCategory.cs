using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using KSP.UI;
using BDArmory.Core;
using BDArmory.Modules;
using BDArmory.Control;
using BDArmory.CounterMeasure;

namespace BDArmory.UI
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class BDAEditorCategory : MonoBehaviour
    {
        public static BDAEditorCategory Instance;
        public PartCategorizer.Category BDACategory;
        public const string BDACategoryKey = "bdacategory";
        public const string AutoBDACategoryKey = "autobdacategory";
        public const int SubcategoryGroup = 412440121;
        /// <summary>
        /// Adding to this dictionary on KSPAddon.Startup.EditorAny will add more bda categories.
        /// </summary>
        public static readonly List<string> Categories = new List<string>
        {
            "All",
            "Control",
            "Guns",
            "Gun turrets",
            "Lasers",
            "Laser turrets",
            "Rocket pods",
            "Rocket turrets",
            "Missiles",
            "Missile turrets",
            "Bombs",
            "Torpedoes",
            "Ammo",
            "Radars",
            "Targeting",
            "Countermeasures",
            "Armor",
        };
        public static readonly Dictionary<string, string> CategoryIcons = new Dictionary<string, string>
        {
            {"All", "BDArmory/Textures/Infinity"},
            {"Control", "BDArmory/Textures/Control"},
            {"Guns", "BDArmory/Textures/Gun"},
            {"Gun turrets", "BDArmory/Textures/GunTurret"},
            {"Lasers", "BDArmory/Textures/LaserIcon"},
            {"Laser turrets", "BDArmory/Textures/LaserTurret"},
            {"Rocket pods", "BDArmory/Textures/Rocket"},
            {"Rocket turrets", "BDArmory/Textures/RocketTurret"},
            {"Missiles", "BDArmory/Textures/Missile"},
            {"Missile turrets", "BDArmory/Textures/MissileTurret"},
            {"Bombs", "BDArmory/Textures/Bomb"},
            {"Torpedoes", "BDArmory/Textures/Torpedo"},
            {"Ammo", "BDArmory/Textures/Ammo"},
            {"Radars", "BDArmory/Textures/Radar"},
            {"Targeting", "BDArmory/Textures/Targeting"},
            {"Countermeasures", "BDArmory/Textures/Countermeasures"},
            {"Armor", "BDArmory/Textures/Defense"},
            {"Misc", "BDArmory/Textures/Misc"},
            {"Legacy", "BDArmory/Textures/icon"},
        };
        private List<PartCategorizerButton> SubcategoryButtons = new List<PartCategorizerButton>();
        private string CurrentCategory = BDArmorySettings.SHOW_CATEGORIES ? "All" : "Legacy";
        private RectTransform BDAPartBar;
        private const float SettingsWidth = 230;
        private const float SettingsHeight = 83;
        private const float SettingsMargin = 18;
        private const float SettingsLineHeight = 22;
        private Rect SettingsWindow = new Rect(0, 0, SettingsWidth, SettingsHeight);
        private bool expanded = false;
        private bool SettingsOpen = false;
        private readonly Vector3 offset = new Vector3(34, 0, 0);

        private void Awake()
        {
            Instance = this;
            bool partsDetected = false;
            using (var parts = PartLoader.LoadedPartsList.GetEnumerator())
                while (parts.MoveNext())
                {
                    if (parts.Current == null || !parts.Current.partPrefab || parts.Current.partConfig == null)
                        continue;
                    if (parts.Current.partConfig.HasValue(BDACategoryKey) || parts.Current.manufacturer == Misc.BDAEditorTools.Manufacturer)
                    {
                        partsDetected = true;
                        GameEvents.onGUIEditorToolbarReady.Add(BDArmoryCategory);
                        break;
                    }
                }
            // Part autocategorization
            if (partsDetected && BDArmorySettings.AUTOCATEGORIZE_PARTS)
                using (var parts = PartLoader.LoadedPartsList.GetEnumerator())
                    while (parts.MoveNext())
                    {
                        if (parts.Current.partConfig == null || parts.Current.partPrefab == null)
                            continue;
                        if (parts.Current.partConfig.HasValue(BDACategoryKey))
                            parts.Current.partConfig.AddValue(AutoBDACategoryKey, parts.Current.partConfig.GetValue(BDACategoryKey));
                        else
                        {
                            ModuleWeapon moduleWeapon;
                            MissileLauncher missileLauncher;
                            if ((moduleWeapon = parts.Current.partPrefab.FindModuleImplementing<ModuleWeapon>()) != null)
                            {
                                if (moduleWeapon.weaponType == "laser")
                                {
                                    if (parts.Current.partPrefab.FindModuleImplementing<ModuleTurret>())
                                        parts.Current.partConfig.AddValue(AutoBDACategoryKey, "Laser turrets");
                                    else
                                        parts.Current.partConfig.AddValue(AutoBDACategoryKey, "Lasers");
                                }
                                else
                                {
                                    if (parts.Current.partPrefab.FindModuleImplementing<ModuleTurret>())
                                        parts.Current.partConfig.AddValue(AutoBDACategoryKey, "Gun turrets");
                                    else
                                        parts.Current.partConfig.AddValue(AutoBDACategoryKey, "Guns");
                                }
                            }
                            else if ((missileLauncher = parts.Current.partPrefab.FindModuleImplementing<MissileLauncher>()) != null)
                            {
                                if (missileLauncher.GetWeaponClass() == Misc.WeaponClasses.Bomb)
                                    parts.Current.partConfig.AddValue(AutoBDACategoryKey, "Bombs");
                                else
                                    parts.Current.partConfig.AddValue(AutoBDACategoryKey, "Missiles");
                            }
                            else if (parts.Current.partPrefab.FindModuleImplementing<MissileTurret>() != null)
                            {
                                parts.Current.partConfig.AddValue(AutoBDACategoryKey, "Missile turrets");
                            }
                            else if (parts.Current.partPrefab.FindModuleImplementing<RocketLauncher>() != null)
                            {
                                if (parts.Current.partPrefab.FindModuleImplementing<ModuleTurret>())
                                    parts.Current.partConfig.AddValue(AutoBDACategoryKey, "Rocket turrets");
                                else
                                    parts.Current.partConfig.AddValue(AutoBDACategoryKey, "Rocket pods");
                            }
                            else if (parts.Current.partPrefab.FindModuleImplementing<ModuleRadar>() != null)
                            {
                                parts.Current.partConfig.AddValue(AutoBDACategoryKey, "Radars");
                            }
                            else if (parts.Current.partPrefab.FindModuleImplementing<ModuleTargetingCamera>() != null)
                            {
                                parts.Current.partConfig.AddValue(AutoBDACategoryKey, "Targeting");
                            }
                            else if (parts.Current.partPrefab.FindModuleImplementing<MissileFire>() != null
                                || parts.Current.partPrefab.FindModuleImplementing<IBDAIControl>() != null)
                            {
                                parts.Current.partConfig.AddValue(AutoBDACategoryKey, "Control");
                            }
                            else if (parts.Current.partPrefab.FindModuleImplementing<ModuleECMJammer>() != null
                                || parts.Current.partPrefab.FindModuleImplementing<CMDropper>() != null)
                            {
                                parts.Current.partConfig.AddValue(AutoBDACategoryKey, "Countermeasures");
                            }
                            else
                            {
                                using (var resource = parts.Current.partPrefab.Resources.GetEnumerator())
                                    while (resource.MoveNext())
                                        // Very dumb check, but right now too lazy to implement a better one
                                        if (resource.Current.resourceName.Contains("Ammo"))
                                            parts.Current.partConfig.AddValue(AutoBDACategoryKey, "Ammo");
                            }
                        }
                    }
        }

        private void OnDestroy()
        {
            GameEvents.onGUIEditorToolbarReady.Remove(BDArmoryCategory);
        }

        public static string GetTexturePath(string category)
        {
            if (CategoryIcons.TryGetValue(category, out string value))
                return value;
            return "BDArmory/Textures/icon";
        }

        private void BDArmoryCategory()
        {
            // BDA Category
            const string customCategoryName = "BDAParts";
            const string customDisplayCategoryName = "Armory";

            PartCategorizer.Category filterByFunctionCategory = PartCategorizer.Instance.filters.Find(f => f.button.categorydisplayName == "#autoLOC_453547");
            if (BDACategory != null && filterByFunctionCategory.subcategories.Contains(BDACategory))
                return;

            Texture2D iconTex = GameDatabase.Instance.GetTexture("BDArmory/Textures/icon", false);
            RUI.Icons.Selectable.Icon icon = new RUI.Icons.Selectable.Icon("BDArmory", iconTex, iconTex, false);

            BDACategory = PartCategorizer.AddCustomSubcategoryFilter(
                filterByFunctionCategory,
                customCategoryName,
                customDisplayCategoryName,
                icon,
                part => PartInCurrentCategory(part)
            );

            BDACategory.button.btnToggleGeneric.onClick.AddListener(CategoryButtonClick);
        }

        private void CategoryButtonClick(UnityEngine.EventSystems.PointerEventData pointerEventData, UIRadioButton.State state, UIRadioButton.CallType callType)
        {
            if (pointerEventData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Right)
            {
                SettingsOpen = true;
                SettingsWindow = new Rect(Input.mousePosition.x, Screen.height - Input.mousePosition.y, SettingsWidth, SettingsHeight);
                pointerEventData.Use();
            }
        }


        private void DrawSettingsWindow(int id)
        {
            GUI.Box(new Rect(0, 0, SettingsWidth, SettingsHeight), "BDA Category Settings");

            if (BDArmorySettings.SHOW_CATEGORIES != (BDArmorySettings.SHOW_CATEGORIES = BDArmorySettings.SHOW_CATEGORIES = GUI.Toggle(
                new Rect(SettingsMargin, SettingsLineHeight * 1.25f, SettingsWidth - (2 * SettingsMargin), SettingsLineHeight),
                BDArmorySettings.SHOW_CATEGORIES,
                "Subcategories"
            )))
            {
                PartCategorizer.Instance.editorPartList.Refresh();
            }
            if (BDArmorySettings.AUTOCATEGORIZE_PARTS != (BDArmorySettings.AUTOCATEGORIZE_PARTS = BDArmorySettings.AUTOCATEGORIZE_PARTS = GUI.Toggle(
                new Rect(SettingsMargin, SettingsLineHeight * 2.25f, SettingsWidth - (2 * SettingsMargin), SettingsLineHeight),
                BDArmorySettings.AUTOCATEGORIZE_PARTS,
                "Autocategorize parts"
            )))
            {
                PartCategorizer.Instance.editorPartList.Refresh();
            }

            BDGUIUtils.RepositionWindow(ref SettingsWindow);
            BDGUIUtils.UseMouseEventInRect(SettingsWindow);
        }

        private void CreateBDAPartBar()
        {
            // Check if we need the special categories
            bool foundLegacy = false;
            bool foundMisc = false;
            List<string> foundCategories = new List<string>();
            using (var parts = PartLoader.LoadedPartsList.GetEnumerator())
                while (parts.MoveNext())
                {
                    if (parts.Current == null || !parts.Current.partPrefab || parts.Current.partConfig == null)
                        continue;
                    string cat = "";
                    if (parts.Current.partConfig.TryGetValue(BDArmorySettings.AUTOCATEGORIZE_PARTS ? AutoBDACategoryKey : BDACategoryKey, ref cat))
                    {
                        if (!Categories.Contains(cat))
                            foundMisc = true;
                        else if (!foundCategories.Contains(cat))
                            foundCategories.Add(cat);
                    }
                    // If part does not have a bdacategory but manufacturer is BDA.
                    else if (parts.Current.manufacturer == Misc.BDAEditorTools.Manufacturer)
                        foundLegacy = true;
                }
            Categories.RemoveAll(s => !foundCategories.Contains(s) && s != "All");
            if (foundMisc && !Categories.Contains("Misc"))
                Categories.Add("Misc");
            if (foundLegacy && !Categories.Contains("Legacy"))
                Categories.Add("Legacy");

            // BDA part category bar
            var BDAPartBarContainer = new GameObject();
            BDAPartBarContainer.name = "BDAPartBarContainer";
            BDAPartBar = BDAPartBarContainer.AddComponent<RectTransform>();
            BDAPartBar.name = "BDAPartBar";
            BDAPartBarContainer.transform.SetParent(PartCategorizer.Instance.transform, false);
            BDAPartBar.anchoredPosition = EditorPanels.Instance.partsEditorModes.panelTransform.anchoredPosition + new Vector2(-212, -126);

            // BDA part category bar background
            // DOESN'T WORK, NOTHING WORKS. :(

            // BDA part category buttons
            PartCategorizer.Category filterByFunctionCategory = PartCategorizer.Instance.filters.Find(f => f.button.categorydisplayName == "#autoLOC_453547");
            Vector3 button_offset = filterByFunctionCategory.subcategories[1].button.transform.position - filterByFunctionCategory.subcategories[0].button.transform.position;
            using (var category = Categories.GetEnumerator())
                while (category.MoveNext())
                {
                    // Since I don't wanna deal with drawing pretty little buttons, we're making categories,
                    // stealing the buttons, and then removing the categories.
                    Texture2D iconTex = GameDatabase.Instance.GetTexture(GetTexturePath(category.Current), false);
                    RUI.Icons.Selectable.Icon icon = new RUI.Icons.Selectable.Icon("BDArmory", iconTex, iconTex, false);
                    string name = category.Current;
                    var categorizer_button = PartCategorizer.AddCustomSubcategoryFilter(
                        filterByFunctionCategory,
                        name,
                        name,
                        icon,
                        part => PartInCurrentCategory(part)
                    );
                    var button = categorizer_button.button;
                    button.btnToggleGeneric.onClick.RemoveAllListeners();
                    button.btnToggleGeneric.onFalse.RemoveAllListeners();
                    button.btnToggleGeneric.onFalseBtn.RemoveAllListeners();
                    button.btnToggleGeneric.onTrue.RemoveAllListeners();
                    button.btnToggleGeneric.onTrueBtn.RemoveAllListeners();
                    button.btnToggleGeneric.SetGroup(412440121);
                    button.transform.SetParent(BDAPartBar, false);
                    button.transform.position += button_offset * (SubcategoryButtons.Count - filterByFunctionCategory.subcategories.Count);
                    categorizer_button.DeleteSubcategory();
                    SubcategoryButtons.Add(button);
                    // Gotta use a saved value, because the enumerator changes the value during the run
                    button.btnToggleGeneric.onTrue.AddListener((x, y) => SetCategory(name));
                }
        }

        private void SetCategory(string category)
        {
            CurrentCategory = category;
            PartCategorizer.Instance.editorPartList.Refresh();
        }

        private bool PartInCurrentCategory(AvailablePart part)
        {
            switch (BDArmorySettings.SHOW_CATEGORIES ? CurrentCategory : "Legacy")
            {
                // A few special cases.
                case "All":
                    return part.partConfig.HasValue(BDArmorySettings.AUTOCATEGORIZE_PARTS ? AutoBDACategoryKey : BDACategoryKey);
                case "Legacy":
                    return part.manufacturer == Misc.BDAEditorTools.Manufacturer;
                case "Misc":
                    {
                        string value = null;
                        return part.partConfig.TryGetValue(BDArmorySettings.AUTOCATEGORIZE_PARTS ? AutoBDACategoryKey : BDACategoryKey, ref value) 
                            && (value == "Misc" || !Categories.Contains(value));
                    }
                default:
                    {
                        string value = null;
                        return part.partConfig.TryGetValue(BDArmorySettings.AUTOCATEGORIZE_PARTS ? AutoBDACategoryKey : BDACategoryKey, ref value) 
                            && value == CurrentCategory;
                    }
            }
        }

        private void ExpandPartSelector(Vector3 offset)
        {
            if (BDAPartBar == null || !BDAPartBar.transform.IsChildOf(PartCategorizer.Instance.transform))
                CreateBDAPartBar();
            else
                BDAPartBar.anchoredPosition += new Vector2(offset.x * 10, 0);
            foreach (Transform child in EditorPanels.Instance.partsEditorModes.panelTransform.parent)
            {
                if (child.name == "PartCategorizer")
                    continue;
                child.position += offset;
            }
        }

        void OnGUI()
        {
            if (BDArmorySettings.SHOW_CATEGORIES && BDACategory.button.activeButton.Value && !expanded)
            {
                ExpandPartSelector(offset);
                expanded = true;
            }
            else if ((!BDACategory.button.activeButton.Value || !BDArmorySettings.SHOW_CATEGORIES) && expanded)
            {
                ExpandPartSelector(-offset);
                expanded = false;
            }

            if (SettingsOpen && Event.current.type == EventType.MouseDown
                && !SettingsWindow.Contains(Event.current.mousePosition))
            {
                SettingsOpen = false;
                BDArmorySetup.SaveConfig();
            }
            if (SettingsOpen)
            {
                SettingsWindow = GUI.Window(9476026, SettingsWindow, DrawSettingsWindow, "", BDArmorySetup.BDGuiSkin.window);
            }
        }
    }
}
