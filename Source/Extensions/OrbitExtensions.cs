using System;

namespace MechJim.Extensions {
  public static class OrbitExtensions {
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

    // normalized prograde direction
    public static Vector3d Prograde(this Orbit o, double UT)
    {
      return o.SwappedOrbitalVelocityAtUT(UT).normalized;
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
