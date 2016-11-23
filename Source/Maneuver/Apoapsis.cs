using System;
using UnityEngine;
using MechJim.Extensions;

namespace MechJim.Maneuver {
    public class Apoapsis : ManeuverBase {

        public Apoapsis(Vessel vessel, Orbit o, double UT, double ApA): base(vessel, o, UT) {
            this.ApA    = ApA;
        }

        public double ApA { get; set; }

        public override Vector3d DeltaV() {
            return o.DeltaVToChangeApoapsis(UT, ApA + o.referenceBody.Radius);
        }
    }
}
