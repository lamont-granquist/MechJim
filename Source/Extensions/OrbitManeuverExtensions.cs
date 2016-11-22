using System;
using UnityEngine;

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

      newPeR = Utils.Clamp(newPeR, 0, radius);
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

      newPeR = Utils.Clamp(newPeR, 0, radius);

      double minDeltaV;
      double maxDeltaV;

      Vector3d pos = o.getRelativePositionAtUT(UT);
      Vector3d vel = o.SwappedOrbitalVelocityAtUT(UT);
      Vector3d horizUnit = o.Horizontal(UT);

      Orbit n = new Orbit();

      if (newPeR > o.PeR) {
        /* raising */
        minDeltaV = 0;
        maxDeltaV = 20;

        /* expoential search to find upper bound on maxDeltaV */
        double pe = o.PeR;
        while (ApoapsisIsHigher(newPeR, pe)) {
          maxDeltaV *= 2;
          n.UpdateFromStateVectors(pos, (vel + maxDeltaV * horizUnit).xzy, o.referenceBody, UT);
          pe = n.PeR;
          if (maxDeltaV > 100000) {
            throw new Exception("OrbitExtensions.DeltaVToChangePeriapsis: ludicrous maxDeltaV");
          }
        }
      } else {
        /* lowering */
        minDeltaV = - horizontalVelocity;
        maxDeltaV = 0;
      }

      Func<double,double> fn = delegate(double dV) {
        n.UpdateFromStateVectors(pos, (vel + dV * horizUnit).xzy, o.referenceBody, UT);
        return n.PeR - newPeR;
      };

      int steps;

      double deltaV = Utils.SecantMethod(fn, minDeltaV, maxDeltaV, 1, 100, out steps);

      return deltaV * horizUnit;
    }

    private static bool ApoapsisIsHigher(double ApR, double than) {
      if (ApR < 0 && than > 0) return true;  /* ApR is hyperbolic which is higher than any parabolic orbit */
      if (ApR > 0 && than < 0) return false;  /* ApR is parabolic which is lower than any hyperbolic orbit */
      return ApR > than;
    }

    /* change Ap */
    public static Vector3d DeltaVToChangeApoapsis(this Orbit o, double UT, double newApR) {
      double radius = o.Radius(UT);
      double progradeVelocity = o.ProgradeVelocity(UT);

      if (newApR > 0)
        newApR = Math.Max(newApR, radius);

      double minDeltaV;
      double maxDeltaV;

      Orbit n = new Orbit();
      Vector3d pos = o.getRelativePositionAtUT(UT);
      Vector3d vel = o.SwappedOrbitalVelocityAtUT(UT);
      Vector3d progradeUnit = o.Prograde(UT);

      if (ApoapsisIsHigher(newApR, o.ApR)) {
        minDeltaV = 0;
        maxDeltaV = 20;

        /* expoential search to find upper bound on maxDeltaV */
        double ap = o.ApR;
        while (ApoapsisIsHigher(newApR, ap)) {
          maxDeltaV *= 2;
          n.UpdateFromStateVectors(pos, (vel + maxDeltaV * progradeUnit).xzy, o.referenceBody, UT);
          ap = n.ApR;
          if (maxDeltaV > 100000) {
            throw new Exception("OrbitExtensions.DeltaVToChangeApoapsis: ludicrous maxDeltaV");
          }
        }

      } else {
        /* lowering */
        minDeltaV = - progradeVelocity;
        maxDeltaV = 0;
      }

      Func<double,double> fn = delegate(double dV) {
        n.UpdateFromStateVectors(pos, (vel + dV * progradeUnit).xzy, o.referenceBody, UT);
        return n.ApR;
      };

      int steps;

      double deltaV = Utils.BinarySearch(fn, ApoapsisIsHigher, minDeltaV, maxDeltaV, newApR, 1, 100, out steps);

      return deltaV * progradeUnit;
    }

    /* change inclination */
    public static Vector3d DeltaVToChangeInclination(this Orbit o, double UT, double newInclination) {
      double latitude = o.referenceBody.GetLatitude(o.SwappedAbsolutePositionAtUT(UT));
      double desiredHeading = Utils.HeadingForInclination(newInclination, latitude);

      Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(UT), o.SwappedOrbitalVelocityAtUT(UT));
      Vector3d eastComponent = actualHorizontalVelocity.magnitude * Math.Sin(Math.PI / 180 * desiredHeading) * o.East(UT);
      Vector3d northComponent = actualHorizontalVelocity.magnitude * Math.Cos(Math.PI / 180 * desiredHeading) * o.North(UT);

      if (Vector3d.Dot(actualHorizontalVelocity, northComponent) < 0)
        northComponent *= -1;

      if (Utils.ClampDegrees180(newInclination) < 0)
        northComponent *= -1;

      Vector3d desiredHorizontalVelocity = eastComponent + northComponent;
      return desiredHorizontalVelocity - actualHorizontalVelocity;
    }

    /* match planes ascending */
    public static Vector3d DeltaVAndTimeToMatchPlanesAscending(this Orbit o, Orbit target, double UT, out double burnUT) {
      burnUT = o.TimeOfAscendingNode(target, UT);
      Vector3d desiredHorizontal = Vector3d.Cross(target.SwappedOrbitNormal(), o.Up(burnUT));
      Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(burnUT), o.SwappedOrbitalVelocityAtUT(burnUT));
      Vector3d desiredHorizontalVelocity = actualHorizontalVelocity.magnitude * desiredHorizontal;
      return desiredHorizontalVelocity - actualHorizontalVelocity;
    }

    /* match planes descending */
    public static Vector3d DeltaVAndTimeToMatchPlanesDescending(this Orbit o, Orbit target, double UT, out double burnUT) {
      burnUT = o.TimeOfDescendingNode(target, UT);
      Vector3d desiredHorizontal = Vector3d.Cross(target.SwappedOrbitNormal(), o.Up(burnUT));
      Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(burnUT), o.SwappedOrbitalVelocityAtUT(burnUT));
      Vector3d desiredHorizontalVelocity = actualHorizontalVelocity.magnitude * desiredHorizontal;
      return desiredHorizontalVelocity - actualHorizontalVelocity;
    }

    /* match planes next node */
    public static Vector3d DeltaVAndTimeToMatchPlanes(this Orbit o, Orbit target, double UT, out double burnUT) {
      Vector3d anDV, dnDV;
      anDV = dnDV = Vector3d.zero;
      double anUT, dnUT;
      anUT = dnUT = 0.0;

      try { dnDV = DeltaVAndTimeToMatchPlanesDescending(o, target, UT, out dnUT); } catch (ArgumentException) { }
      try { anDV = DeltaVAndTimeToMatchPlanesAscending(o, target, UT, out anUT); } catch (ArgumentException) { }

      if ( dnUT > Planetarium.GetUniversalTime() && ((anUT < Planetarium.GetUniversalTime()) || (dnUT < anUT )) ) {
        burnUT = dnUT;
        return dnDV;
      } else if ( anUT > Planetarium.GetUniversalTime() && ((dnUT < Planetarium.GetUniversalTime()) || (anUT < dnUT )) ) {
        burnUT = anUT;
        return anDV;
      } else {
        throw new ArgumentException("DeltaVAndTimeToMatchPlanesDescending: no AN/DN in the future");
      }
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
