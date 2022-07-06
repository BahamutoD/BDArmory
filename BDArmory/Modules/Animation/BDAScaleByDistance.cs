using UnityEngine;

namespace BDArmory.Modules.Animation
{
    public class BDAScaleByDistance : PartModule
    {
        [KSPField(isPersistant = false)] public string transformToScaleName;

        public Transform transformToScale;

        [KSPField(isPersistant = false)] public string scaleFactor = "0,0,1";
        Vector3 scaleFactorV;

        [KSPField(isPersistant = false)] public string distanceTransformName;

        public Transform distanceTransform;

        public override void OnStart(StartState state)
        {
            ParseScale();
            transformToScale = part.FindModelTransform(transformToScaleName);
            distanceTransform = part.FindModelTransform(distanceTransformName);
        }

        public void FixedUpdate()
        {
            Vector3 finalScaleFactor;
            float distance = Vector3.Distance(transformToScale.position, distanceTransform.position);
            float sfX = (scaleFactorV.x != 0) ? scaleFactorV.x * distance : 1;
            float sfY = (scaleFactorV.y != 0) ? scaleFactorV.y * distance : 1;
            float sfZ = (scaleFactorV.z != 0) ? scaleFactorV.z * distance : 1;
            finalScaleFactor = new Vector3(sfX, sfY, sfZ);

            transformToScale.localScale = finalScaleFactor;
        }

        void ParseScale()
        {
            string[] split = scaleFactor.Split(',');
            float[] splitF = new float[split.Length];
            for (int i = 0; i < split.Length; i++)
            {
                splitF[i] = float.Parse(split[i]);
            }
            scaleFactorV = new Vector3(splitF[0], splitF[1], splitF[2]);
        }
    }
}
