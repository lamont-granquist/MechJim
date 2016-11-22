using System;
using UnityEngine;
using MechJim.Extensions;

namespace MechJim.Maneuver {
  public class Ellipticize : ManeuverBase {

    public Ellipticize(Vessel vessel, Orbit o, double UT, double PeA, double ApA): base(vessel, o, UT) {
      this.PeA    = PeA;
      this.ApA    = ApA;
    }

    public double PeA { get; set; }
    public double ApA { get; set; }

    public override Vector3d DeltaV() {
      return o.DeltaVToEllipticize(UT, PeA + o.referenceBody.Radius, ApA + o.referenceBody.Radius);
    }
  }
}
