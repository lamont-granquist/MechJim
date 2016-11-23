using System;
using UnityEngine;
using MechJim.Extensions;

namespace MechJim.Maneuver {
    public class Circularize : ManeuverBase {
        public Circularize(Vessel vessel, Orbit o, double UT): base(vessel, o, UT) { }

        public override Vector3d DeltaV() {
            return o.DeltaVToCircularize(UT);
        }
    }
}
