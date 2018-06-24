using System.Collections.Generic;

namespace BDArmory.Radar
{
  public class ModuleSpaceRadar : ModuleRadar
  {

    public void Update() // runs every frame
    {
      if (HighLogic.LoadedSceneIsFlight) // if in the flight scene
      {
        UpdateRadar(); // run the UpdateRadar code
      }
    }

    // This code determines if the radar is below the cutoff altitude and if so then
    // it disables the radar ... private so that it cannot be accessed by any other code
    private void UpdateRadar()
    {
      if (vessel.atmDensity >= 0.007) // below an atm density of 0.007 the radar will not work
      {
        List<ModuleSpaceRadar> radarParts = new List<ModuleSpaceRadar>(200); // creates a list of parts with this module

        foreach (Part p in vessel.Parts) // checks each part in the vessel 
        {
          radarParts.AddRange(p.FindModulesImplementing<ModuleSpaceRadar>()); // adds the part to the list if this module is present in the part
        }
        foreach (ModuleSpaceRadar radarPart in radarParts) // for each of the parts in the list do the following
        {
          if (radarPart != null && radarPart.radarEnabled)
          {
            DisableRadar(); // disable the radar
          }
        }
      }
    }
  }
}