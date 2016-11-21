using System;
using static MechJim.Utils;

namespace MechJim.Extensions {
  public static class OrbitManeuverExtensions {

    /* circularization */
    public static Vector3d DeltaVToCircularize(this Orbit o, double UT) {
      Vector3d desiredVelocity = o.referenceBody.CircVelocityAtRadius(o.Radius(UT)) * o.Horizontal(UT);
      return desiredVelocity - o.SwappedOrbitalVelocityAtUT(UT);
    }

    /* change both Pe and Ap */
    public static Vector3d DeltaVToEllipticize(this Orbit o, double UT, double newPeR, double newApR) {
      double radius = o.Radius(UT);

      newPeR = Clamp(newPeR, 0, radius);
      newApR = Math.Max(newApR, radius);

      double mu = o.referenceBody.gravParameter;
      double E = - mu / ( newPeR + newApR); // total energy per mass
      double L = Math.Sqrt(Math.Abs((Math.Pow(E * (newApR - newPeR), 2) - mu * mu ) / ( 2 * E ))); // angular momentum
      double Ek = E + mu / radius; // kinetic energy per mass
      double Vh = L / radius; // new horizontal velocity
      double Vu = Math.Sqrt(Math.Abs(2 * Ek - Vh * Vh)); // new vertical velocity

      Vector3d desiredVelocity = Vh * o.Horizontal(UT) + Vu * o.Up(UT);

      return desiredVelocity - o.SwappedOrbitalVelocityAtUT(UT);
    }

    /* change Pe */
    public static Vector3d DeltaVToChangePeriapsis(this Orbit o, double UT, double newPeR) {
      double radius = o.Radius(UT);
      double horizontalVelocity = o.HorizontalVelocity(UT);

      newPeR = Clamp(newPeR, 0, radius);

      double minDeltaV;
      double maxDeltaV;

      if (newPeR > o.PeR) {
        /* raising */
        minDeltaV = 0;
        maxDeltaV = o.EscapeVelocity(UT) - horizontalVelocity;
      } else {
        /* lowering */
        minDeltaV = - horizontalVelocity;
        maxDeltaV = 0;
      }

      Vector3d pos = o.getRelativePositionAtUT(UT);
      Vector3d vel = o.SwappedOrbitalVelocityAtUT(UT);
      Vector3d horizUnit = o.Horizontal(UT);

      Orbit n = new Orbit();

      Func<double,double> fn = delegate(double dV) {
        n.UpdateFromStateVectors(pos, (vel + dV * horizUnit).xzy, o.referenceBody, UT);
        return n.PeR - newPeR;
      };

      double deltaV = BinarySearch(fn, minDeltaV, maxDeltaV, 1, 100);

      return deltaV * horizUnit;
    }

    /* change Ap */
    public static Vector3d DeltaVToChangeApoapsis(this Orbit o, double UT, double newApR) {
      /* FIXME: implement */
      return Vector3d.zero;
    }

    /* change inclination */
    public static Vector3d DeltaVToChangeInclination(this Orbit o, double UT, double newInclination) {
      /* FIXME: implement */
      return Vector3d.zero;
    }

    /* match planes ascending */
    public static Vector3d DeltaVAndTimeToMatchPlanesAscending(this Orbit o, Orbit target, double UT, out double burnUT) {
      /* FIXME: implement */
      burnUT = Planetarium.GetUniversalTime();
      return Vector3d.zero;
    }

    /* match planes descending */
    public static Vector3d DeltaVAndTimeToMatchPlanesDescending(this Orbit o, Orbit target, double UT, out double burnUT) {
      /* FIXME: implement */
      burnUT = Planetarium.GetUniversalTime();
      return Vector3d.zero;
    }

    /* hohmann transfer */
    private static Vector3d DeltaVAndApsisPhaseAngleOfHohmannTransfer(this Orbit o, Orbit target, double UT, out double apsisPhaseAngle) {
      /* FIXME: implement */
      apsisPhaseAngle = 0;
      return Vector3d.zero;
    }

    public static Vector3d DeltaVAndTimeForHohmannTransfer(this Orbit o, Orbit target, double UT, out double burnUT) {
      /* FIXME: implement */
      burnUT = Planetarium.GetUniversalTime();
      return Vector3d.zero;
    }

    /* moon return */
    public static Vector3d DeltaVAndTimeForMoonReturnEjection(this Orbit o, double UT, double targetPrimaryRadius, out double burnUT) {
      CelestialBody moon = o.referenceBody;
      CelestialBody primary = moon.referenceBody;

      Orbit primaryOrbit = new Orbit(moon.orbit.inclination, moon.orbit.eccentricity, targetPrimaryRadius, moon.orbit.LAN, moon.orbit.argumentOfPeriapsis, moon.orbit.meanAnomalyAtEpoch, moon.orbit.epoch, primary);

      /* FIXME: needs Transfer Injections + Lambert Solver */
      burnUT = Planetarium.GetUniversalTime();
      return Vector3d.zero;
    }

    /* match velocities */
    public static Vector3d DeltaVToMatchVelocities(this Orbit o, double UT, Orbit target) {
      return target.SwappedOrbitalVelocityAtUT(UT) - o.SwappedOrbitalVelocityAtUT(UT);
    }

    /* resonant orbit */
    public static Vector3d DeltaVToResonantOrbit(this Orbit o, double UT, double f) {
      double a = o.ApR;
      double p = o.PeR;

      double x = Math.Pow(Math.Pow(a, 3) * Math.Pow(f, 2) + 3 * Math.Pow(a, 2) * Math.Pow(f, 2) * p + 3 * a * Math.Pow(f, 2) * Math.Pow(p, 2) + Math.Pow(f, 2) * Math.Pow(p, 3), 1d / 3) - a;

      if (x < 0)
        return Vector3d.zero;

      if (f > 1)
        return DeltaVToChangeApoapsis(o, UT, x);
      else
        return DeltaVToChangePeriapsis(o, UT, x);
    }

  }
}
