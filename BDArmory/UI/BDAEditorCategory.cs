using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;

namespace BDArmory.UI
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class BDAEditorCategory : MonoBehaviour
    {
        public static BDAEditorCategory Instance;
        public PartCategorizer.Category BDACategory;
        public const string BDACategoryKey = "bdacategory";
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
            "Laser turrets",
            "Rocket pods",
            "Rocket turrets",
            "Missiles",
            "Missile turrets",
            "Bombs",
            "Ammo",
            "Radars",
            "Countermeasures",
            "Armor",
        };
        public static readonly Dictionary<string, string> CategoryIcons = new Dictionary<string, string>
        {
            {"All", "BDArmory/Textures/Infinity"},
            {"Control", "BDArmory/Textures/Control"},
            {"Guns", "BDArmory/Textures/Gun"},
            {"Gun turrets", "BDArmory/Textures/GunTurret"},
            {"Laser turrets", "BDArmory/Textures/LaserTurret"},
            {"Rocket pods", "BDArmory/Textures/Rocket"},
            {"Rocket turrets", "BDArmory/Textures/RocketTurret"},
            {"Missiles", "BDArmory/Textures/Missile"},
            {"Missile turrets", "BDArmory/Textures/MissileTurret"},
            {"Bombs", "BDArmory/Textures/Bomb"},
            {"Ammo", "BDArmory/Textures/Ammo"},
            {"Radars", "BDArmory/Textures/Radar"},
            {"Countermeasures", "BDArmory/Textures/Countermeasures"},
            {"Armor", "BDArmory/Textures/Defense"},
            {"Misc", "BDArmory/Textures/Misc"},
            {"Legacy", "BDArmory/Textures/icon"},
        };
        private List<PartCategorizerButton> SubcategoryButtons = new List<PartCategorizerButton>();
        private string CurrentCategory = "All";
        private RectTransform BDAPartBar;
        private bool expanded = false;
        private readonly Vector3 offset = new Vector3(34, 0, 0);

        private void Awake()
        {
            Instance = this;
            using (var parts = PartLoader.LoadedPartsList.GetEnumerator())
                while (parts.MoveNext())
                {
                    if (parts.Current == null || !parts.Current.partPrefab || parts.Current.partConfig == null)
                        continue;
                    if (parts.Current.partConfig.HasValue(BDACategoryKey) || parts.Current.manufacturer == Misc.BDAEditorTools.Manufacturer)
                    {
                        GameEvents.onGUIEditorToolbarReady.Add(BDArmoryCategory);
                        break;
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
        }

        private void CreateBDAPartBar()
        {
            // Check if we need the special categories
            bool foundLegacy = false;
            bool foundMisc = false;
            using (var parts = PartLoader.LoadedPartsList.GetEnumerator())
                while (parts.MoveNext())
                {
                    if (parts.Current == null || !parts.Current.partPrefab || parts.Current.partConfig == null)
                        continue;
                    string cat = "";
                    if (parts.Current.partConfig.TryGetValue(BDACategoryKey, ref cat))
                    {
                        if (!Categories.Contains(cat))
                            foundMisc = true;
                    }
                    // If part does not have a bdacategory but manufacturer is BDA.
                    else if (parts.Current.manufacturer == Misc.BDAEditorTools.Manufacturer)
                        foundLegacy = true;
                }
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
            switch (CurrentCategory)
            {
                // A few special cases.
                case "All":
                    return part.partConfig.HasValue(BDACategoryKey);
                case "Legacy":
                    return part.manufacturer == Misc.BDAEditorTools.Manufacturer && !part.partConfig.HasValue(BDACategoryKey);
                case "Misc":
                    {
                        string value = null;
                        return part.partConfig.TryGetValue(BDACategoryKey, ref value) && (value == "Misc" || !Categories.Contains(value));
                    }
                default:
                    {
                        string value = null;
                        return part.partConfig.TryGetValue(BDACategoryKey, ref value) && value == CurrentCategory;
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
            if (BDACategory.button.activeButton.Value && !expanded)
            {
                ExpandPartSelector(offset);
                expanded = true;
            }
            else if (!BDACategory.button.activeButton.Value && expanded)
            {
                ExpandPartSelector(-offset);
                expanded = false;
            }
        }
    }
}
