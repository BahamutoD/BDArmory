using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;

namespace BahaTurret
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class BDAEditorCategory : MonoBehaviour
	{
		private static readonly List<AvailablePart> availableParts = new List<AvailablePart>();

		void Awake()
		{
			GameEvents.onGUIEditorToolbarReady.Add(BDAWeaponsCategory);

			//availableParts.Clear();
			//availableParts.AddRange(PartLoader.LoadedPartsList.BDAParts());

		}

	    void BDAWeaponsCategory()
	    {
	        const string customCategoryName = "BDAWeapons";
	        const string customDisplayCategoryName = "BDA Weapons";

	        availableParts.Clear();
	        availableParts.AddRange(PartLoader.LoadedPartsList.BDAParts());

	        Texture2D iconTex = GameDatabase.Instance.GetTexture("BDArmory/Textures/icon", false);

	        RUI.Icons.Selectable.Icon icon = new RUI.Icons.Selectable.Icon("BDArmory", iconTex, iconTex, false);

	        PartCategorizer.Category filter = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == "Filter by function");

	        PartCategorizer.AddCustomSubcategoryFilter(filter, customCategoryName, customDisplayCategoryName, icon,
	            p => availableParts.Contains(p));

	    }
    }
}

