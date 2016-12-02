/* portions plagiarized from MechJeb */

using System;
using UnityEngine;

namespace MechJim.Extensions {
    public static class OrbitExtensions {
        public static Vector3d SwappedVelocityAtPeriapsis(this Orbit o) {
            return o.SwappedOrbitalVelocityAtUT(o.timeToPe + Planetarium.GetUniversalTime());
        }

        public static Vector3d SwappedVelocityAtApoapsis(this Orbit o) {
            return o.SwappedOrbitalVelocityAtUT(o.timeToAp + Planetarium.GetUniversalTime());
        }

        /*
         * goofy KSP xzy swapping
         */

        // orbital velocity
        public static Vector3d SwappedOrbitalVelocityAtUT(this Orbit o, double UT) {
            return o.getOrbitalVelocityAtUT(UT).xzy;
        }

        // position relative to primary
        public static Vector3d SwappedRelativePositionAtUT(this Orbit o, double UT) {
            return o.getRelativePositionAtUT(UT).xzy;
        }

        // position in world-space
        public static Vector3d SwappedAbsolutePositionAtUT(this Orbit o, double UT) {
            return o.referenceBody.position + o.SwappedRelativePositionAtUT(UT);
        }

        // normal vector
        public static Vector3d SwappedOrbitNormal(this Orbit o) {
            return o.GetOrbitNormal().xzy.normalized;
        }

        /*
         * unit vectors and velocities
         */

        // normalized prograde direction
        public static Vector3d Prograde(this Orbit o, double UT)
        {
            return o.SwappedOrbitalVelocityAtUT(UT).normalized;
        }

        // magnitude of the velocity in prograde direction
        public static double ProgradeVelocity(this Orbit o, double UT)
        {
            return o.SwappedOrbitalVelocityAtUT(UT).magnitude;
        }

        // normalized radial-out direction perpendicular to the surface
        public static Vector3d Up(this Orbit o, double UT)
        {
            return o.SwappedRelativePositionAtUT(UT).normalized;
        }

        // normalized direction of the radial-out navball direction
        public static Vector3d Radial(this Orbit o, double UT) {
            return Vector3d.Exclude(o.Prograde(UT), o.Up(UT)).normalized;
        }

        // normalized direction of the normal navball direction
        public static Vector3d Normal(this Orbit o, double UT) {
            return o.SwappedOrbitNormal();
        }

        // normalized direction of the prograde vector projected parallel to the surface
        public static Vector3d Horizontal(this Orbit o, double UT)
        {
            return Vector3d.Exclude(o.Up(UT), o.Prograde(UT)).normalized;
        }

        // magnitude of the velocity in the horizontal direction
        public static double HorizontalVelocity(this Orbit o, double UT)
        {
            return Vector3d.Dot(o.SwappedOrbitalVelocityAtUT(UT), o.Horizontal(UT));
        }

        // normalized direction pointing north projected parallel to the surface
        public static Vector3d North(this Orbit o, double UT)
        {
            return Vector3d.Exclude(o.Up(UT), Planetarium.up).normalized;
        }

        // normalized direction pointing east projected parallel to the surface
        public static Vector3d East(this Orbit o, double UT)
        {
            return Vector3d.Cross(o.Up(UT), o.North(UT)).normalized;
        }

        /*
         * Times
         */

        // this can return a result in the past for hyperbolic orbits
        public static double TimeOfPeriapsis(this Orbit o, double UT) { /* MechJeb: NextPeriapsisTime() */
            if (o.eccentricity < 1)
                return o.TimeOfTrueAnomaly(0, UT);
            else
                return UT - o.MeanAnomalyAtUT(UT) / o.MeanMotion();
        }

        // throws ArgumentException for hyperbolic orbits
        public static double TimeOfApoapsis(this Orbit o, double UT) { /* MechJeb: NextApoapsisTime() */
            if (o.eccentricity < 1)
                return o.TimeOfTrueAnomaly(180, UT);
            else
                throw new ArgumentException("NextApoapsisTime called on a hyperbolic orbit");
        }

        // can throw ArgumentException on hyperbolic orbits
        public static double TimeOfTrueAnomaly(this Orbit o, double trueAnomaly, double UT) {
            return o.UTAtMeanAnomaly(o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly)), UT);
        }

        // can throw ArgumentException on hyperbolic orbits
        public static double TimeOfAscendingNode(this Orbit a, Orbit b, double UT) {
            return a.TimeOfTrueAnomaly(a.AscendingNodeTrueAnomaly(b), UT);
        }

        // can throw ArgumentException on hyperbolic orbits
        public static double TimeOfDescendingNode(this Orbit a, Orbit b, double UT) {
            return a.TimeOfTrueAnomaly(a.DescendingNodeTrueAnomaly(b), UT);
        }

        public static double TimeOfAscendingNodeEquatorial(this Orbit o, double UT) {
            return o.TimeOfTrueAnomaly(o.AscendingNodeEquatorialTrueAnomaly(), UT);
        }

        public static double TimeOfDescendingNodeEquatorial(this Orbit o, double UT) {
            return o.TimeOfTrueAnomaly(o.DescendingNodeEquatorialTrueAnomaly(), UT);
        }

        public static double TimeOfRadius(this Orbit o, double UT, double radius) { /* MechJeb: NextTimeOfRadius() */
            if (radius < o.PeR || (o.eccentricity < 1 && radius > o.ApR))
                throw new ArgumentException("OrbitExtensions.NextTimeOfRadius: given radius of " + radius + " is never achieved: o.PeR = " + o.PeR + " and o.ApR = " + o.ApR);

            double trueAnomaly1 = 180 / Math.PI * o.TrueAnomalyAtRadius(radius);
            double trueAnomaly2 = 360 - trueAnomaly1;
            double time1 = o.TimeOfTrueAnomaly(trueAnomaly1, UT);
            double time2 = o.TimeOfTrueAnomaly(trueAnomaly2, UT);

            if (time2 < time1 && time2 > UT)
                return time2;
            else
                return time1;
        }

        /*
         * Anomalies
         */

        public static double AscendingNodeTrueAnomaly(this Orbit a, Orbit b) {
            Vector3d vectorToAN = Vector3d.Cross(a.SwappedOrbitNormal(), b.SwappedOrbitNormal());
            return a.TrueAnomalyFromVector(vectorToAN);
        }

        public static double DescendingNodeTrueAnomaly(this Orbit a, Orbit b) {
            return Utils.ClampDegrees360(a.AscendingNodeTrueAnomaly(b) + 180);
        }

        public static double AscendingNodeEquatorialTrueAnomaly(this Orbit o) {
            Vector3d vectorToAN = Vector3d.Cross(o.referenceBody.transform.up, o.SwappedOrbitNormal());
            return o.TrueAnomalyFromVector(vectorToAN);
        }

        public static double DescendingNodeEquatorialTrueAnomaly(this Orbit o) {
            return Utils.ClampDegrees360(o.AscendingNodeEquatorialTrueAnomaly() + 180);
        }

        public static double MaximumTrueAnomaly(this Orbit o) {
            if (o.eccentricity < 1) return 180;
            else return 180 / Math.PI * Math.Acos(-1 / o.eccentricity);
        }

        public static bool AscendingNodeExists(this Orbit a, Orbit b) {
            return Math.Abs(Utils.ClampDegrees180(a.AscendingNodeTrueAnomaly(b))) <= a.MaximumTrueAnomaly();
        }

        public static bool DescendingNodeExists(this Orbit a, Orbit b) {
            return Math.Abs(Utils.ClampDegrees180(a.DescendingNodeTrueAnomaly(b))) <= a.MaximumTrueAnomaly();
        }

        public static bool AscendingNodeEquatorialExists(this Orbit o) {
            return Math.Abs(Utils.ClampDegrees180(o.AscendingNodeEquatorialTrueAnomaly())) <= o.MaximumTrueAnomaly();
        }

        public static bool DescendingNodeEquatorialExists(this Orbit o) {
            return Math.Abs(Utils.ClampDegrees180(o.DescendingNodeEquatorialTrueAnomaly())) <= o.MaximumTrueAnomaly();
        }

        public static Vector3d SwappedRelativePositionAtPeriapsis(this Orbit o) {
            Vector3d vectorToAN = Quaternion.AngleAxis(-(float)o.LAN, Planetarium.up) * Planetarium.right;
            Vector3d vectorToPe = Quaternion.AngleAxis((float)o.argumentOfPeriapsis, o.SwappedOrbitNormal()) * vectorToAN;
            return o.PeR * vectorToPe;
        }

        public static Vector3d SwappedRelativePositionAtApoapsis(this Orbit o) {
            Vector3d vectorToAN = Quaternion.AngleAxis(-(float)o.LAN, Planetarium.up) * Planetarium.right;
            Vector3d vectorToPe = Quaternion.AngleAxis((float)o.argumentOfPeriapsis, o.SwappedOrbitNormal()) * vectorToAN;
            Vector3d ret = -o.ApR * vectorToPe;
            if (double.IsNaN(ret.x))
            {
                Debug.LogError("OrbitExtensions.SwappedRelativePositionAtApoapsis got a NaN result!");
                Debug.LogError("o.LAN = " + o.LAN);
                Debug.LogError("o.inclination = " + o.inclination);
                Debug.LogError("o.argumentOfPeriapsis = " + o.argumentOfPeriapsis);
                Debug.LogError("o.SwappedOrbitNormal() = " + o.SwappedOrbitNormal());
            }
            return ret;
        }

        public static double TrueAnomalyFromVector(this Orbit o, Vector3d vec)
        {
            Vector3d oNormal = o.SwappedOrbitNormal();
            Vector3d projected = Vector3d.Exclude(oNormal, vec);
            Vector3d vectorToPe = o.SwappedRelativePositionAtPeriapsis();
            double angleFromPe = Vector3d.Angle(vectorToPe, projected);

            //If the vector points to the infalling part of the orbit then we need to do 360 minus the
            //angle from Pe to get the true anomaly. Test this by taking the the cross product of the
            //orbit normal and vector to the periapsis. This gives a vector that points to center of the
            //outgoing side of the orbit. If vectorToAN is more than 90 degrees from this vector, it occurs
            //during the infalling part of the orbit.
            if (Math.Abs(Vector3d.Angle(projected, Vector3d.Cross(oNormal, vectorToPe))) < 90) {
                return angleFromPe;
            } else {
                return 360 - angleFromPe;
            }
        }

        public static double UTAtMeanAnomaly(this Orbit o, double meanAnomaly, double UT) {
            double currentMeanAnomaly = o.MeanAnomalyAtUT(UT);
            double meanDifference = meanAnomaly - currentMeanAnomaly;

            if (o.eccentricity < 1)
                meanDifference = Utils.ClampRadiansTwoPi(meanDifference);
            return UT + meanDifference / o.MeanMotion();
        }

        public static double MeanAnomalyAtUT(this Orbit o, double UT) {
            // We use ObtAtEpoch and not meanAnomalyAtEpoch because somehow meanAnomalyAtEpoch
            // can be wrong when using the RealSolarSystem mod. ObtAtEpoch is always correct.
            double ret = (o.ObTAtEpoch + (UT - o.epoch)) * o.MeanMotion();
            if (o.eccentricity < 1)
                ret = Utils.ClampRadiansTwoPi(ret);
            return ret;
        }

        public static double GetMeanAnomalyAtEccentricAnomaly(this Orbit o, double E) {
            double e = o.eccentricity;
            if (e < 1) {
                return Utils.ClampRadiansTwoPi(E - (e * Math.Sin(E)));
            } else {
                return (e * Math.Sinh(E)) - E;
            }
        }

        public static double GetEccentricAnomalyAtTrueAnomaly(this Orbit o, double trueAnomaly)
        {
            double e = o.eccentricity;
            trueAnomaly = Utils.ClampDegrees360(trueAnomaly);
            trueAnomaly = trueAnomaly * (Math.PI / 180);

            if (e < 1) {
                double cosE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
                double sinE = Math.Sqrt(1 - (cosE * cosE));
                if (trueAnomaly > Math.PI)
                    sinE *= -1;

                return Utils.ClampRadiansTwoPi(Math.Atan2(sinE, cosE));
            } else {
                double coshE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
                if (coshE < 1)
                    throw new ArgumentException("OrbitExtensions.GetEccentricAnomalyAtTrueAnomaly: True anomaly of " + trueAnomaly + " radians is not attained by orbit with eccentricity " + o.eccentricity);

                double E = Utils.Acosh(coshE);
                if (trueAnomaly > Math.PI)
                    E *= -1;

                return E;
            }
        }

        public static double GetMeanAnomalyAtTrueAnomaly(this Orbit o, double trueAnomaly)
        {
            return o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly));
        }

        /*
         * Misc
         */

        public static double MeanMotion(this Orbit o) {
            if (o.eccentricity > 1) {
                // FIXME: how can RSS break this???  bug maybe fixed in RSS?
                return Math.Sqrt(o.referenceBody.gravParameter / Math.Abs(Math.Pow(o.semiMajorAxis, 3)));
            } else {
                // The above formula is wrong when using the RealSolarSystem mod, which messes with orbital periods.
                // This simpler formula should be foolproof for elliptical orbits:
                return 2 * Math.PI / o.period;
            }
        }

        // distance from the center of the primary
        public static double Radius(this Orbit o, double UT) {
            return o.SwappedRelativePositionAtUT(UT).magnitude;
        }

        // escape velocity
        public static double EscapeVelocity(this Orbit o, double UT) {
            return Math.Sqrt(2 * o.referenceBody.gravParameter / o.Radius(UT));
        }

        public static Vector3d DeltaVToManeuverNodeCoordinates(this Orbit o, double UT, Vector3d dV) {
            return new Vector3d(
                    Vector3d.Dot(o.Radial(UT), dV),
                    Vector3d.Dot(-o.Normal(UT), dV),
                    Vector3d.Dot(o.Prograde(UT), dV)
                    );
        }
    }
}
