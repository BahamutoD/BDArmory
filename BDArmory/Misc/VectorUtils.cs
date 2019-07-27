using System;
using UnityEngine;

namespace BDArmory.Misc
{
    public static class VectorUtils
    {
        private static System.Random RandomGen = new System.Random();

        /// <param name="referenceRight">Right compared to fromDirection, make sure it's not orthogonal to toDirection, or you'll get unstable signs</param>
        public static float SignedAngle(Vector3 fromDirection, Vector3 toDirection, Vector3 referenceRight)
        {
            float angle = Vector3.Angle(fromDirection, toDirection);
            float sign = Mathf.Sign(Vector3.Dot(toDirection, referenceRight));
            float finalAngle = sign * angle;
            return finalAngle;
        }

        /// <summary>
        /// Same as SignedAngle, just using double precision for the cosine calculation.
        /// For very small angles the floating point precision starts to matter, as the cosine is close to 1, not to 0.
        /// </summary>
        public static float SignedAngleDP(Vector3 fromDirection, Vector3 toDirection, Vector3 referenceRight)
        {
            float angle = (float)Vector3d.Angle(fromDirection, toDirection);
            float sign = Mathf.Sign(Vector3.Dot(toDirection, referenceRight));
            float finalAngle = sign * angle;
            return finalAngle;
        }
        
        /// <summary>
        /// Convert an angle to be between -180 and 180.
        /// </summary>
        public static float ToAngle(this float angle)
        {
            angle = (angle + 180) % 360;
            return angle > 0 ? angle - 180 : angle + 180;
        }

        //from howlingmoonsoftware.com
        //calculates how long it will take for a target to be where it will be when a bullet fired now can reach it.
        //delta = initial relative position, vr = relative velocity, muzzleV = bullet velocity.
        public static float CalculateLeadTime(Vector3 delta, Vector3 vr, float muzzleV)
        {
            // Quadratic equation coefficients a*t^2 + b*t + c = 0
            float a = Vector3.Dot(vr, vr) - muzzleV * muzzleV;
            float b = 2f * Vector3.Dot(vr, delta);
            float c = Vector3.Dot(delta, delta);

            float det = b * b - 4f * a * c;

            // If the determinant is negative, then there is no solution
            if (det > 0f)
            {
                return 2f * c / (Mathf.Sqrt(det) - b);
            }
            else
            {
                return -1f;
            }
        }

        /// <summary>
        /// Returns a value between -1 and 1 via Perlin noise.
        /// </summary>
        /// <returns>Returns a value between -1 and 1 via Perlin noise.</returns>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        public static float FullRangePerlinNoise(float x, float y)
        {
            float perlin = Mathf.PerlinNoise(x, y);

            perlin -= 0.5f;
            perlin *= 2;

            return perlin;
        }

        public static Vector3 RandomDirectionDeviation(Vector3 direction, float maxAngle)
        {
            return Vector3.RotateTowards(direction, UnityEngine.Random.rotation * direction, UnityEngine.Random.Range(0, maxAngle * Mathf.Deg2Rad), 0).normalized;
        }

        public static Vector3 WeightedDirectionDeviation(Vector3 direction, float maxAngle)
        {
            float random = UnityEngine.Random.Range(0f, 1f);
            float maxRotate = maxAngle * (random * random);
            maxRotate = Mathf.Clamp(maxRotate, 0, maxAngle) * Mathf.Deg2Rad;
            return Vector3.RotateTowards(direction, Vector3.ProjectOnPlane(UnityEngine.Random.onUnitSphere, direction), maxRotate, 0).normalized;
        }

        /// <summary>
        /// Returns the original vector rotated in a random direction using the give standard deviation.
        /// </summary>
        /// <param name="direction">mean direction</param>
        /// <param name="standardDeviation">standard deviation in degrees</param>
        /// <returns>Randomly adjusted Vector3</returns>
        /// <remarks>
        /// Technically, this is calculated using the chi-squared distribution in polar coordinates,
        /// which, incidentally, makes the math easier too.
        /// However a chi-squared (k=2) distance from center distribution produces a vector distributed normally
        /// on any chosen axis orthogonal to the original vector, which is exactly what we want.
        /// </remarks>
        public static Vector3 GaussianDirectionDeviation(Vector3 direction, float standardDeviation)
        {
            return Quaternion.AngleAxis(UnityEngine.Random.Range(-180f, 180f), direction)
                * Quaternion.AngleAxis(Rayleigh() * standardDeviation,
                                       new Vector3(-1 / direction.x, -1 / direction.y, 2 / direction.z))  // orthogonal vector
                * direction;
        }

        /// <returns>Random float distributed with an approximated standard normal distribution</returns>
        /// <see>https://en.wikipedia.org/wiki/Box%E2%80%93Muller_transform</see>
        /// <remarks>Note a standard normal variable is technically unbounded</remarks>
        public static float Gaussian()
        {
            // Technically this will raise an exception if the first random produces a zero
            try
            {
                return Mathf.Sqrt(-2 * Mathf.Log(UnityEngine.Random.value)) * Mathf.Cos(Mathf.PI * UnityEngine.Random.value);
            }
            catch (Exception)
            { // I have no idea what exception Mathf.Log raises when it gets a zero
                return 0;
            }
        }

        /// <returns>
        /// Random float distributed with the chi-squared distribution with two degrees of freedom
        /// aka the Rayleigh distribution.
        /// Multiply by deviation for best results.
        /// </returns>
        /// <see>https://en.wikipedia.org/wiki/Rayleigh_distribution</see>
        /// <remarks>Note a chi-square distributed variable is technically unbounded</remarks>
        public static float Rayleigh()
        {
            // Technically this will raise an exception if the random produces a zero, which should almost never happen
            try
            {
                return Mathf.Sqrt(-2 * Mathf.Log(UnityEngine.Random.value));
            }
            catch (Exception)
            { // I have no idea what exception Mathf.Log raises when it gets a zero
                return 0;
            }
        }

        /// <summary>
        /// Converts world position to Lat,Long,Alt form.
        /// </summary>
        /// <returns>The position in geo coords.</returns>
        /// <param name="worldPosition">World position.</param>
        /// <param name="body">Body.</param>
        public static Vector3d WorldPositionToGeoCoords(Vector3d worldPosition, CelestialBody body)
        {
            if (!body)
            {
                //Debug.Log ("BahaTurret.VectorUtils.WorldPositionToGeoCoords body is null");
                return Vector3d.zero;
            }

            double lat = body.GetLatitude(worldPosition);
            double longi = body.GetLongitude(worldPosition);
            double alt = body.GetAltitude(worldPosition);
            return new Vector3d(lat, longi, alt);
        }

        /// <summary>
        /// Calculates the coordinates of a point a certain distance away in a specified direction.
        /// </summary>
        /// <param name="start">Starting point coordinates, in Lat,Long,Alt form</param>
        /// <param name="body">The body on which the movement is happening</param>
        /// <param name="bearing">Bearing to move in, in degrees, where 0 is north and 90 is east</param>
        /// <param name="distance">Distance to move, in meters</param>
        /// <returns>Ending point coordinates, in Lat,Long,Alt form</returns>
        public static Vector3 GeoCoordinateOffset(Vector3 start, CelestialBody body, float bearing, float distance)
        {
            //https://stackoverflow.com/questions/2637023/how-to-calculate-the-latlng-of-a-point-a-certain-distance-away-from-another
            float lat1 = start.x * Mathf.Deg2Rad;
            float lon1 = start.y * Mathf.Deg2Rad;
            bearing *= Mathf.Deg2Rad;
            distance /= ((float)body.Radius + start.z);

            float lat2 = Mathf.Asin(Mathf.Sin(lat1) * Mathf.Cos(distance) + Mathf.Cos(lat1) * Mathf.Sin(distance) * Mathf.Cos(bearing));
            float lon2 = lon1 + Mathf.Atan2(Mathf.Sin(bearing) * Mathf.Sin(distance) * Mathf.Cos(lat1), Mathf.Cos(distance) - Mathf.Sin(lat1) * Mathf.Sin(lat2));

            return new Vector3(lat2 * Mathf.Rad2Deg, lon2 * Mathf.Rad2Deg, start.z);
        }

        /// <summary>
        /// Calculate the bearing going from one point to another
        /// </summary>
        /// <param name="start">Starting point coordinates, in Lat,Long,Alt form</param>
        /// <param name="destination">Destination point coordinates, in Lat,Long,Alt form</param>
        /// <returns>Bearing when looking at destination from start, in degrees, where 0 is north and 90 is east</returns>
        public static float GeoForwardAzimuth(Vector3 start, Vector3 destination)
        {
            //http://www.movable-type.co.uk/scripts/latlong.html
            float lat1 = start.x * Mathf.Deg2Rad;
            float lon1 = start.y * Mathf.Deg2Rad;
            float lat2 = destination.x * Mathf.Deg2Rad;
            float lon2 = destination.y * Mathf.Deg2Rad;
            return Mathf.Atan2(Mathf.Sin(lon2 - lon1) * Mathf.Cos(lat2), Mathf.Cos(lat1) * Mathf.Sin(lat2) - Mathf.Sin(lat1) * Mathf.Cos(lat2) * Mathf.Cos(lon2 - lon1)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Calculate the distance from one point to another on a globe
        /// </summary>
        /// <param name="start">Starting point coordinates, in Lat,Long,Alt form</param>
        /// <param name="destination">Destination point coordinates, in Lat,Long,Alt form</param>
        /// <param name="body">The body on which the distance is calculated</param>
        /// <returns>distance between the two points</returns>
        public static float GeoDistance(Vector3 start, Vector3 destination, CelestialBody body)
        {
            //http://www.movable-type.co.uk/scripts/latlong.html
            float lat1 = start.x * Mathf.Deg2Rad;
            float lat2 = destination.x * Mathf.Deg2Rad;
            float dlat = lat2 - lat1;
            float dlon = (destination.y - start.y) * Mathf.Deg2Rad;
            float a = Mathf.Sin(dlat / 2) * Mathf.Sin(dlat / 2) + Mathf.Cos(lat1) * Mathf.Cos(lat2) * Mathf.Sin(dlon / 2) * Mathf.Sin(dlon / 2);
            float distance = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a)) * (float)body.Radius;
            return Mathf.Sqrt(distance * distance + (destination.z - start.z) * (destination.z - start.z));
        }

        public static Vector3 RotatePointAround(Vector3 pointToRotate, Vector3 pivotPoint, Vector3 axis, float angle)
        {
            Vector3 line = pointToRotate - pivotPoint;
            line = Quaternion.AngleAxis(angle, axis) * line;
            return pivotPoint + line;
        }

        public static Vector3 GetNorthVector(Vector3 position, CelestialBody body)
        {
            Vector3 geoPosA = WorldPositionToGeoCoords(position, body);
            Vector3 geoPosB = new Vector3(geoPosA.x + 1, geoPosA.y, geoPosA.z);
            Vector3 north = GetWorldSurfacePostion(geoPosB, body) - GetWorldSurfacePostion(geoPosA, body);
            return Vector3.ProjectOnPlane(north, body.GetSurfaceNVector(geoPosA.x, geoPosA.y)).normalized;
        }

        public static Vector3 GetWorldSurfacePostion(Vector3d geoPosition, CelestialBody body)
        {
            if (!body)
            {
                return Vector3.zero;
            }
            return body.GetWorldSurfacePosition(geoPosition.x, geoPosition.y, geoPosition.z);
        }

        public static Vector3 GetUpDirection(Vector3 position)
        {
            if (FlightGlobals.currentMainBody == null) return Vector3.up;
            return (position - FlightGlobals.currentMainBody.transform.position).normalized;
        }

        public static bool SphereRayIntersect(Ray ray, Vector3 sphereCenter, double sphereRadius, out double distance)
        {
            Vector3 o = ray.origin;
            Vector3 l = ray.direction;
            Vector3d c = sphereCenter;
            double r = sphereRadius;

            double d;

            d = -(Vector3.Dot(l, o - c) + Math.Sqrt(Mathf.Pow(Vector3.Dot(l, o - c), 2) - (o - c).sqrMagnitude + (r * r)));

            if (double.IsNaN(d))
            {
                distance = 0;
                return false;
            }
            else
            {
                distance = d;
                return true;
            }
        }
    }
}
