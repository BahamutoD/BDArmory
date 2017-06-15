using UnityEngine;

namespace BDArmory.Armor
{
    public static class ArmorPenetration
    {
        public static void DoPenetrationRay(BulletPenetrationData data, float positiveCoefficient = 0)
        {
            Ray ray = data.rayIn;
            RaycastHit hit = data.hitResultIn;
            Vector3 finalDirect = Vector3.Lerp(ray.direction, -hit.normal, positiveCoefficient);
            float maxDis = hit.collider.bounds.size.magnitude;
            Vector3 point = finalDirect*maxDis + hit.point;
            Ray ray1 = new Ray(point, -finalDirect);
            RaycastHit hit1;
            if (hit.collider.Raycast(ray1, out hit1, maxDis))
            {
                data.rayOut = new Ray(point, -finalDirect);
                data.hitResultOut = hit1;
                data.armorThickness = Vector3.Distance(hit.point, hit1.point);
                return;
            }
            data.armorThickness = float.MaxValue;
        }

        public class BulletPenetrationData
        {
            public Ray rayIn;
            public RaycastHit hitResultIn;
            public Ray rayOut;
            public RaycastHit hitResultOut;
            public float armorThickness;

            public BulletPenetrationData(Ray ray, RaycastHit hitResult)
            {
                rayIn = ray;
                hitResultIn = hitResult;
            }
        }
    }
}