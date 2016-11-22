using System;
using UnityEngine;
using MechJim.Extensions;

namespace MechJim.Maneuver {
  public class Periapsis : ManeuverBase {

    public Periapsis(Vessel vessel, Orbit o, double UT, double PeA): base(vessel, o, UT) {
      this.PeA    = PeA;
    }

    public double PeA { get; set; }

    public override Vector3d DeltaV() {
      return o.DeltaVToChangePeriapsis(UT, PeA + o.referenceBody.Radius);
    }
  }
}
