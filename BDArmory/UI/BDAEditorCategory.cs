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
        public static PartCategorizer.Category BDACategory;
        public static PartCategorizer BDAPartBar;
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
        private static UnityEngine.RectTransform bgim;

        private void Awake()
        {
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

            // BDA part category bar background
            foreach (var child in EditorPanels.Instance.partsEditorModes.panelTransform.parent.Find("PartCategorizer").Find("BackgroundRight").GetComponents<MonoBehaviour>())
            {
                Debug.Log(child.name);
                Debug.Log(child.GetType());
                if (child is UnityEngine.UI.Image image)
                {
                    Debug.Log(image.sprite.texture);

                    //bgim = EditorPanels.Instance.partsEditorModes.panelTransform.parent.gameObject.AddComponent<UnityEngine.UI.Image>();
                    //Texture2D tex = new Texture2D(image.sprite.texture.width, image.sprite.texture.height);
                    //tex.SetPixels(image.sprite.texture.GetPixels());
                    //bgim.sprite = Sprite.Create(tex, image.sprite.rect, image.sprite.pivot);
                    //bgim.transform.position = EditorPanels.Instance.partsEditorModes.panelTransform.parent.Find("PartCategorizer").position + offset + offset + offset;
                }
            }
            bgim = Instantiate<RectTransform>((RectTransform)EditorPanels.Instance.partsEditorModes.panelTransform.parent.Find("PartCategorizer").Find("BackgroundRight"));
            bgim.SetParent(EditorPanels.Instance.partsEditorModes.panelTransform.parent);
            // Debug.Log(EditorPanels.Instance.partsEditor.panelTransform == EditorPanels.Instance.partsEditorModes.panelTransform.parent.parent); // true

            // BDA part category buttons
            string value = null;
            var another_button = PartCategorizer.AddCustomSubcategoryFilter(
                filterByFunctionCategory,
                "Guns",
                "Guns",
                icon,
                part => part.partConfig.TryGetValue("bdacategory", ref value) && value == "Guns"
            );
            another_button.button.transform.position += offset;
            //BDACategory.subcategories.Add(another_button);
            //PartCategorizer.Instance.categories.Remove(another_button);
            filterByFunctionCategory.subcategories.Remove(another_button);
            //filterByFunctionCategory.RebuildSubcategoryButtons();

            //var BDAPartBar;
        }

        private void ExpandPartSelector(Vector3 offset)
        {
            foreach (Transform child in EditorPanels.Instance.partsEditorModes.panelTransform.parent)
            {
                if (child.name == "PartCategorizer")
                {
                    foreach (Transform subchild in child)
                    {
                        Debug.Log($"{subchild.name}: {subchild.position} - {subchild.GetType()}");
                        if (subchild.name == "BackgroundRight")
                        {
                            Debug.Log("+++BEGIN++++");
                            foreach (Transform subsubchild in subchild)
                                Debug.Log($"{subsubchild.name}: {subsubchild.position} - {subsubchild.GetType()}");
                            Debug.Log("+++COMP++++");
                            foreach (MonoBehaviour subsubchild in subchild.GetComponents<MonoBehaviour>())
                                Debug.Log($"{subsubchild.name}:  - {subsubchild.GetType()}");
                            Debug.Log("+++OBJ++++");
                            foreach (UnityEngine.Object subsubchild in subchild.GetComponents<UnityEngine.Object>())
                                Debug.Log($"{subsubchild.name}:  - {subsubchild.GetType()}");
                            Debug.Log("+++END++++");
                        }
                    }
                }

                if (child.name == "PartCategorizer")
                    continue;
                child.position += offset;
            }
            Debug.Log("---------------");
            Debug.Log(bgim.transform.position);
            bgim.transform.position = EditorPanels.Instance.partsEditorModes.panelTransform.parent.Find("PartCategorizer").Find("BackgroundRight").position + offset * 10;
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
