using System;
using UnityEngine;
using MechJim.Extensions;

namespace MechJim.Maneuver {
  public class Circularize : ManeuverBase {

    public Circularize(Vessel vessel, Orbit o, double UT) {
      this.vessel = vessel;
      this.o = o;
      this.UT = UT;
    }

    public Vessel vessel { get; set; }
    public Orbit o { get; set; }
    public double UT { get; set; }

    public Vector3d DeltaV() {
      Vector3d desiredVelocity = o.referenceBody.CircVelocityAtRadius(o.Radius(UT)) * o.Horizontal(UT);
      Vector3d actualVelocity = o.SwappedOrbitalVelocityAtUT(UT);
      return desiredVelocity - actualVelocity;
    }

    public ManeuverNode PlaceManeuverNode() {
      return vessel.PlaceManeuverNode(o, DeltaV(), UT);
    }
  }
}
