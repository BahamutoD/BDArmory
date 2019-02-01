using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace BDArmory.UI
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class BDAEditorCategory : MonoBehaviour
    {
        public static BDAEditorCategory Instance;
        public PartCategorizer.Category BDACategory;
        private RectTransform BDAPartBar;
        private PartCategorizer.Category another_button;
        public string CurrentCategory = "Guns";
        public static readonly List<string> Categories = new List<string>
        {
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
        private bool expanded = false;
        private readonly Vector3 offset = new Vector3(30, 0, 0);

        private void Awake()
        {
            Instance = this;
            GameEvents.onGUIEditorToolbarReady.Add(BDArmoryCategory);
        }

        private void BDArmoryCategory()
        {
            // BDA Category
            const string customCategoryName = "BDAParts";
            const string customDisplayCategoryName = "Armory";

            Texture2D iconTex = GameDatabase.Instance.GetTexture("BDArmory/Textures/icon", false);
            RUI.Icons.Selectable.Icon icon = new RUI.Icons.Selectable.Icon("BDArmory", iconTex, iconTex, false);

            PartCategorizer.Category filterByFunctionCategory = PartCategorizer.Instance.filters.Find(f => f.button.categorydisplayName == "#autoLOC_453547");

            BDACategory = PartCategorizer.AddCustomSubcategoryFilter(
                filterByFunctionCategory,
                customCategoryName,
                customDisplayCategoryName,
                icon,
                part => part.partConfig.HasValue("bdacategory")
            );
        }

        private void CreateBDAPartBar()
        {
            // BDA part category bar background
            var dummy = new GameObject();
            BDAPartBar = dummy.AddComponent<RectTransform>();
            BDAPartBar.name = "BDAPartBar";
            dummy.transform.SetParent(PartCategorizer.Instance.transform, false);
            BDAPartBar.anchoredPosition = EditorPanels.Instance.partsEditorModes.panelTransform.anchoredPosition + new Vector2(offset.x, offset.y);
            // DOESN'T WORK, NOTHING WORKS. :(

            // BDA part category buttons
            PartCategorizer.Category filterByFunctionCategory = PartCategorizer.Instance.filters.Find(f => f.button.categorydisplayName == "#autoLOC_453547");
            Texture2D iconTex = GameDatabase.Instance.GetTexture("BDArmory/Textures/icon", false);
            RUI.Icons.Selectable.Icon icon = new RUI.Icons.Selectable.Icon("BDArmory", iconTex, iconTex, false);
            string value = null;
            another_button = PartCategorizer.AddCustomSubcategoryFilter(
                filterByFunctionCategory,
                "Guns",
                "Guns",
                icon,
                part => part.partConfig.TryGetValue("bdacategory", ref value) && value == CurrentCategory
            );
            var button = another_button.button;
            button.OnBtnTap.Clear();
            //button.btnGeneric.enabled = false;
            //button.btnToggleGeneric.enabled = false;
            //button.btnGeneric.onClick.RemoveAllListeners();
            button.btnToggleGeneric.onClick.RemoveAllListeners();
            button.btnToggleGeneric.onFalse.RemoveAllListeners();
            button.btnToggleGeneric.onFalseBtn.RemoveAllListeners();
            button.btnToggleGeneric.onTrue.RemoveAllListeners();
            button.btnToggleGeneric.onTrueBtn.RemoveAllListeners();
            button.btnToggleGeneric.SetGroup(412440121);
            button.transform.SetParent(BDAPartBar, false);
            another_button.DeleteSubcategory();
            //filterByFunctionCategory.subcategories.Remove(another_button);
            //filterByFunctionCategory.RebuildSubcategoryButtons();

            //var BDAPartBar;
        }

        private void SetCategory(string category)
        {
            CurrentCategory = category;
            PartCategorizer.Instance.editorPartList.Refresh();
        }

        private void ExpandPartSelector(Vector3 offset)
        {
            if (BDAPartBar == null)
                CreateBDAPartBar();
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
