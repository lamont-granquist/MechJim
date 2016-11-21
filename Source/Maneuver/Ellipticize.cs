using System;
using UnityEngine;
using MechJim.Extensions;

namespace MechJim.Maneuver {
  public class Ellipticize : ManeuverBase {

    public Ellipticize(Vessel vessel, Orbit o, double UT, double PeR, double ApR): base(vessel, o, UT) {
      this.PeR    = PeR;
      this.ApR    = ApR;
    }

    public double PeR { get; set; }
    public double ApR { get; set; }

    public override Vector3d DeltaV() {
      return o.DeltaVToEllipticize(UT, PeR, ApR);
    }
  }
}
