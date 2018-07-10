using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BDArmory.Core;
using BDArmory.Core.Extension;


using KSP.UI.Screens;
using UniLinq;
using UnityEngine;
using BDArmory.Core.Utils;

namespace BDArmory
{
    public class ModuleEMP : PartModule
    {
        [KSPField]
        public float empRange;
        [KSPField]
        public bool isEMPed = false;

        public override void OnUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }
            //Timer code for effect?
            
        }

        void CheckEMP()
        {

        }

        void ApplyEMP()
        {

        }
    }
}
