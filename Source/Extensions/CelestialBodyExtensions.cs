using System;

namespace MechJim.Extensions {
  public static class CelestialBodyExtensions {
    public static double CircVelocityAtRadius(this CelestialBody body, double radius) {
      return Math.Sqrt(body.gravParameter / radius);
    }
  }
}
